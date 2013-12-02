using System;
using System.Linq;

namespace Vici.CoolStorage
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ToStringAttribute : Attribute
    {
    }
}