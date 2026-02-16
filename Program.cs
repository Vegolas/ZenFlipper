using System;
using System.Text;

namespace Gothic2ZenFlipper;

class Program
{
    static void Main(string[] args)
    {
        // Register Windows-1250 encoding for Central European characters (Gothic 2)
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        
        ZenFlipper.Run(args);
    }
}
