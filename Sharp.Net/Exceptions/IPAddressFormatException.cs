using Sharp.Net.Localization;
using System;

namespace Sharp.Net.Exceptions
{
    public class IPAddressFormatException : FormatException
    {
        public IPAddressFormatException(string? address) : base(string.Format(ExceptionMessages.WrongAddressFormat, address)) { }
    }
}
