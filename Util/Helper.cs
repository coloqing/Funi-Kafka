using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Util
{
    public class Helper
    {
        /// <summary>
        /// MD5加密
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string GetMD5String(string str)
        {
            using MD5 md5 = MD5.Create(); 
            byte[] b = Encoding.UTF8.GetBytes(str);
            byte[] md5b = md5.ComputeHash(b);

            StringBuilder sb = new StringBuilder();
            foreach (var item in md5b)
            {
                sb.Append(item.ToString("x2"));
            }

            return sb.ToString();
        }

        /// <summary>
        /// 根据线路号获取json数据
        /// </summary>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="InvalidOperationException"></exception>

        public static Dictionary<string, string> LoadJsonData(string xlh)
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "trainNumber.json");
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("The file does not exist.", filePath);
            }

            string fileContent = File.ReadAllText(filePath);
            if (string.IsNullOrEmpty(fileContent))
            {
                throw new InvalidOperationException("The file is empty.");
            }

            try
            {
                var data = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(fileContent);
                return (Dictionary<string, string>)data[xlh];
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error parsing JSON: {ex.Message}");
                // 可以选择返回null、空字典或其他默认值  
                return new Dictionary<string, string>();
            }
        }
    }
}
