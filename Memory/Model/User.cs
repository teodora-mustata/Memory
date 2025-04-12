using System;
using System.Xml.Serialization;

namespace Memory.Model
{
    [Serializable]
    public class User
    {
        public string Name { get; set; }
        public string AvatarPath { get; set; }

        public User() { } 

        public User(string name, string avatarPath)
        {
            Name = name;
            AvatarPath = avatarPath;
        }
    }

}
