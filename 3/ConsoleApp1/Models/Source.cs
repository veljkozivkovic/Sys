using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Models
{
    public class Source
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public Source(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
