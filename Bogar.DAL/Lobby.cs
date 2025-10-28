﻿namespace Bogar.DAL
{
    public class Lobby
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
