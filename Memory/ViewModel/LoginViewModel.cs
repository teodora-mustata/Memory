using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Xml.Serialization;
using Microsoft.Win32;
using Memory.Model;
using Microsoft.VisualBasic;
using System.Windows;

namespace Memory.ViewModel
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private const string UsersFile = "users.xml";

        private User _selectedUser;
        public ObservableCollection<User> Users { get; set; }

        public User SelectedUser
        {
            get { return _selectedUser; }
            set
            {
                _selectedUser = value;
                OnPropertyChanged(nameof(SelectedUser));
                OnPropertyChanged(nameof(CanDeleteOrPlay));
            }
        }

        public bool CanDeleteOrPlay => SelectedUser != null;

        public ICommand AddUserCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand PlayCommand { get; }

        public LoginViewModel()
        {
            Users = new ObservableCollection<User>();
            LoadUsers();

            AddUserCommand = new RelayCommand(AddUser);
            DeleteUserCommand = new RelayCommand(DeleteUser, () => CanDeleteOrPlay);
            PlayCommand = new RelayCommand(PlayGame, () => CanDeleteOrPlay);

        }

        private void AddUser()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files (*.jpg;*.png;*.gif)|*.jpg;*.png;*.gif",
                Title = "Select an avatar",
                InitialDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "profilePics")
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string userName = Interaction.InputBox("Enter username:", "New User");

                if (string.IsNullOrWhiteSpace(userName)) return; 

                Users.Add(new User(userName, openFileDialog.FileName));
                SaveUsers();
            }
        }

        private void DeleteUser()
        {
            if (_selectedUser != null)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to permanently delete user '{_selectedUser.Name}' and all associated data?",
                    "Confirm Deletion",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    DeleteUserData(_selectedUser.Name);
                    Users.Remove(_selectedUser);
                    _selectedUser = null;
                    SaveUsers();
                }
            }
        }

        private void PlayGame()
        {
            var gameWindow = new View.GameWindow(_selectedUser.Name);
            gameWindow.Show();
        }


        private void SaveUsers()
        {
            try
            {
                using (var writer = new StreamWriter(UsersFile))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(ObservableCollection<User>));
                    serializer.Serialize(writer, Users);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving users: " + ex.Message);
            }
        }

        private void LoadUsers()
        {
            if (File.Exists(UsersFile))
            {
                try
                {
                    using (var reader = new StreamReader(UsersFile))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(ObservableCollection<User>));
                        Users = (ObservableCollection<User>)serializer.Deserialize(reader);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error loading users: " + ex.Message);
                }
            }
        }


        private void DeleteUserData(string username)
        {
            const string accountsFile = "users.xml";
            if (File.Exists(accountsFile))
            {
                var serializer = new XmlSerializer(typeof(List<User>));
                List<User> accounts;
                using (var stream = new FileStream(accountsFile, FileMode.Open))
                {
                    accounts = (List<User>)serializer.Deserialize(stream);
                }

                accounts.RemoveAll(acc => acc.Name == username);

                using (var stream = new FileStream(accountsFile, FileMode.Create))
                {
                    serializer.Serialize(stream, accounts);
                }
            }

            const string statsFile = "user_stats.xml";
            if (File.Exists(statsFile))
            {
                var serializer = new XmlSerializer(typeof(AllUserStats));
                AllUserStats stats;
                using (var stream = new FileStream(statsFile, FileMode.Open))
                {
                    stats = (AllUserStats)serializer.Deserialize(stream);
                }

                stats.Users.RemoveAll(stat => stat.Username == username);

                using (var stream = new FileStream(statsFile, FileMode.Create))
                {
                    serializer.Serialize(stream, stats);
                }
            }

            const string savedGamesFile = "saved_games.xml";
            if (File.Exists(savedGamesFile))
            {
                var serializer = new XmlSerializer(typeof(AllSavedGames));
                AllSavedGames allGames;
                using (var stream = new FileStream(savedGamesFile, FileMode.Open))
                {
                    allGames = (AllSavedGames)serializer.Deserialize(stream);
                }

                allGames.Games.RemoveAll(g => g.Username == username);

                using (var stream = new FileStream(savedGamesFile, FileMode.Create))
                {
                    serializer.Serialize(stream, allGames);
                }
            }

            MessageBox.Show($"All data for user '{username}' has been deleted.", "Account Deleted", MessageBoxButton.OK, MessageBoxImage.Information);
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }



    }
}
