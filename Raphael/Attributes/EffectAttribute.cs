using System;

namespace Raphael.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class EffectAttribute : Attribute
    {
        public string Name { get; }
        public string? Description { get; set; }
        public int Order { get; set; } = 999;
        public bool Enabled { get; set; } = true;
        public string? Category { get; set; }
        public string? Icon { get; set; }

        public EffectAttribute(string name)
        {
            Name = name;
        }
    }
}