using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q2C.Model
{
    public class FastaFile
    {
        public int Id { get; set; } = -1;
        public string Name { get; set; }
        public string Path { get; set; }
        public bool IsSelected { get; set; }

        public FastaFile(int id, string name, string path, bool isSelected)
        {
            Id = id;
            Name = name;
            Path = path;
            IsSelected = isSelected;
        }
    }
}
