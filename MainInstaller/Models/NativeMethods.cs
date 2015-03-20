using System.Runtime.InteropServices;

namespace Installer.Models
{
    class NativeMethods
    {
        internal static bool IsWindowsProductTypeEqual(byte wProductType, ushort wSuiteMask = 0)
        {
            Osversioninfoexw oSVERSIONINFOEXW = default(Osversioninfoexw);
            oSVERSIONINFOEXW.dwOSVersionInfoSize = (uint)Marshal.SizeOf(typeof(Osversioninfoexw));
            oSVERSIONINFOEXW.wProductType = wProductType;
            oSVERSIONINFOEXW.wSuiteMask = wSuiteMask;
            ulong dwlConditionMask = 0uL;
            uint num = 0u;
            dwlConditionMask = VerSetConditionMask(dwlConditionMask, TypeMask.VER_PRODUCT_TYPE, ConditionMask.VER_EQUAL);
            num |= TypeMask.VER_PRODUCT_TYPE;
            if (wSuiteMask != 0)
            {
                dwlConditionMask = VerSetConditionMask(dwlConditionMask, TypeMask.VER_SUITENAME, ConditionMask.VER_AND);
                num |= TypeMask.VER_SUITENAME;
            }
            return VerifyVersionInfoW(ref oSVERSIONINFOEXW, num, dwlConditionMask);
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern ulong VerSetConditionMask(ulong dwlConditionMask, uint dwTypeBitMask, byte dwConditionMask);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool VerifyVersionInfoW(ref Osversioninfoexw lpVersionInfo, uint dwTypeMask, ulong dwlConditionMask);

        private static class TypeMask
        {
            internal static uint VER_MINORVERSION = 1u;
            internal static uint VER_MAJORVERSION = 2u;
            internal static uint VER_BUILDNUMBER = 4u;
            internal static uint VER_PLATFORMID = 8u;
            internal static uint VER_SERVICEPACKMINOR = 16u;
            internal static uint VER_SERVICEPACKMAJOR = 32u;
            internal static uint VER_SUITENAME = 64u;
            internal static uint VER_PRODUCT_TYPE = 128u;
        }

        private static class ConditionMask
        {
            internal static byte VER_EQUAL = 1;
            internal static byte VER_GREATER = 2;
            internal static byte VER_GREATER_EQUAL = 3;
            internal static byte VER_LESS = 4;
            internal static byte VER_LESS_EQUAL = 5;
            internal static byte VER_AND = 6;
            internal static byte VER_OR = 7;
        }
    }
}