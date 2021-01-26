using System;
using System.Text;
using System.Runtime.InteropServices;

namespace NetPublic
{
    public class IniUtil
    {
        private string m_IniPath;

        public IniUtil(string path)
        {
            this.m_IniPath = path;  //INI 파일 위치를 생성할때 인자로 넘겨 받음
        }

        [DllImport("kernel32.dll")]
        private static extern int GetPrivateProfileString(    // GetIniValue 를 위해
            String section,
            String key,
            String def,
            StringBuilder retVal,
            int size,
            String filePath);



        [DllImport("kernel32.dll")]
        private static extern Int64 WritePrivateProfileString(  // SetIniValue를 위해
            String section,
            String key,
            String val,
            String filePath);


        // INI 값을 읽어 온다. 
        public String GetIniValue(String Section, String Key)
        {
            try
            {
                StringBuilder temp = new StringBuilder(255);
                int i = GetPrivateProfileString(Section, Key, "", temp, 255, m_IniPath);
                return temp.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetIniValue:{ex.Message}");
            }

            return "";
        }

        // INI 값을 셋팅
        public void SetIniValue(String Section, String Key, String Value)
        {
            try
            {
                WritePrivateProfileString(Section, Key, Value, m_IniPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SetIniValue:{ex.Message}");
            }
        }
    }
}
