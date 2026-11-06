using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Shared
{
    public static class Lang
    {
        const string Untranslated = "(REQUIRES TRANSLATION";

        public static readonly string[] Languages = new string[]
        {
            "en", "es", "fr", "pt", "ptbr", "ru", "ro", "id", "bhs", "cs"
        };

        static readonly Dictionary<string, Dictionary<string, string>> Codes = new Dictionary<string, Dictionary<string, string>>();
        static readonly Dictionary<string, Dictionary<string, string>> Sentences = new Dictionary<string, Dictionary<string, string>>();

        static Lang()
        {
            LangData.Fill(Codes, Sentences);
        }

        public static string ItemType(string language, string uname)
        {
            var item = Catalogue.GetItemType(uname);
            if (item == null)
                return uname ?? "";
            return TextFor(language, item.Name);
        }

        public static string Text(string language, string key)
        {
            return Text(language, key, new object[0]);
        }

        public static string Text(string language, string key, params object[] args)
        {
            return Format(LookupCode(language, key), args);
        }

        public static string TextFor(string language, string english)
        {
            if (english == null)
                return "";
            Dictionary<string, string> table;
            string value;
            if (language != null && Sentences.TryGetValue(language, out table) && table.TryGetValue(english, out value) && value != null)
                return value;
            return english;
        }

        static string LookupCode(string language, string key)
        {
            Dictionary<string, string> table;
            string value;
            if (language != null && Codes.TryGetValue(language, out table) && table.TryGetValue(key, out value))
            {
                if (value != null && !value.StartsWith(Untranslated))
                    return value;
            }
            if (Codes.TryGetValue("en", out table) && table.TryGetValue(key, out value) && value != null && !value.StartsWith(Untranslated))
                return value;
            return key ?? "";
        }

        static string Format(string template, object[] args)
        {
            if (template == null)
                return "";
            if (args == null || args.Length == 0)
                return template;
            try
            {
                var sb = new StringBuilder();
                int ai = 0;
                for (int i = 0; i < template.Length; i++)
                {
                    char c = template[i];
                    if (c != '%' || i + 1 >= template.Length)
                    {
                        sb.Append(c);
                        continue;
                    }
                    char n = template[i + 1];
                    if (n == '%')
                    {
                        sb.Append('%');
                        i++;
                        continue;
                    }
                    if (n == 's')
                    {
                        sb.Append(args[ai++]);
                        i++;
                        continue;
                    }
                    if (n == 'd')
                    {
                        sb.Append(Convert.ToInt64(args[ai++], CultureInfo.InvariantCulture));
                        i++;
                        continue;
                    }
                    if (n == 'f')
                    {
                        sb.Append(Convert.ToDouble(args[ai++], CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture));
                        i++;
                        continue;
                    }
                    if (n == '.')
                    {
                        int j = i + 2;
                        int prec = 0;
                        while (j < template.Length && template[j] >= '0' && template[j] <= '9')
                        {
                            prec = prec * 10 + (template[j] - '0');
                            j++;
                        }
                        if (j < template.Length && template[j] == 'f')
                        {
                            double v = Convert.ToDouble(args[ai++], CultureInfo.InvariantCulture);
                            sb.Append(v.ToString("F" + prec, CultureInfo.InvariantCulture));
                            i = j;
                            continue;
                        }
                    }
                    sb.Append(c);
                }
                return sb.ToString();
            }
            catch (Exception)
            {
                return template;
            }
        }
    }
}
