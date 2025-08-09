using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q2C.Model
{
    public class ComboBoxItem
    {
        public string Name { get; set; }

        public bool IsChecked { get; set; }

        public ComboBoxItem(string name)
        {
            Name = name;
        }

        public ComboBoxItem(string name, bool isChecked)
        {
            Name = name;
            IsChecked = isChecked;
        }
    }
}
