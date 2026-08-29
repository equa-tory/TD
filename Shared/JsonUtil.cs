namespace Tokenizer.Shared
{
    public static class JsonUtil
    {
        // Extracts "key":"value" or "key":123 from a flat JSON string
        public static string GetString(string json, string key)
        {
            string search = "\"" + key + "\"";
            int ki = json.IndexOf(search);
            if (ki < 0) return null;

            int colon = json.IndexOf(':', ki + search.Length);
            if (colon < 0) return null;

            int start = colon + 1;
            while (start < json.Length && json[start] == ' ') start++;

            if (json[start] == '"')
            {
                // string value
                int end = json.IndexOf('"', start + 1);
                return json.Substring(start + 1, end - start - 1);
            }
            else
            {
                // numeric / bool / null value
                int end = start;
                while (end < json.Length && json[end] != ',' && json[end] != '}') end++;
                string raw = json.Substring(start, end - start).Trim();
                // JSON null -> real null, so callers don't try to parse the literal "null"
                if (raw == "null") return null;
                return raw;
            }
        }
    }
}