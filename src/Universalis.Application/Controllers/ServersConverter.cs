using System;
using System.ComponentModel;
using System.Globalization;

namespace Universalis.Application.Controllers;

public class ServersConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, 
        CultureInfo culture, object value)
    {
        if (value is string s)
        {
            if (Servers.TryParse(s, out var servers))
            {
                return servers;
            }
        }
        
        return base.ConvertFrom(context, culture, value);
    }
}