﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

// Credit: http://stackoverflow.com/questions/104850/c-test-if-string-is-a-guid-without-throwing-exceptions
namespace PInvoke
{
    class ObjBase
    {
        /// <summary>
        /// This function converts a string generated by the StringFromCLSID function back into the original class identifier.
        /// </summary>
        /// <param name="sz">String that represents the class identifier</param>
        /// <param name="clsid">On return will contain the class identifier</param>
        /// <returns>
        /// Positive or zero if class identifier was obtained successfully
        /// Negative if the call failed
        /// </returns>
        [DllImport("ole32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = true)]
        public static extern int CLSIDFromString(string sz, out Guid clsid);
    }
}