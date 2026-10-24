using System;
using System.Text.RegularExpressions;
using System.Web;

namespace tabseetCourse.Classes
{
    /// <summary>
    /// آلية موحدة وآمنة للتعرف على روابط YouTube واستخراج معرّف الفيديو وبناء رابط المشاهدة الموحد ورابط التضمين.
    /// تدعم: youtube.com/watch?v=ID ، youtu.be/ID ، youtube.com/shorts/ID ، youtube.com/live/ID ،
    /// youtube.com/embed/ID ، روابط الجوال m.youtube.com ، كود iframe الكامل، والمعاملات الإضافية (t= / start= / si= / list=).
    /// لا يُقبل أي نطاق غير النطاقات الرسمية (youtube.com وفروعه، youtube-nocookie.com، youtu.be).
    /// </summary>
    public static class YouTubeLinkHelper
    {
        public sealed class YouTubeLinkInfo
        {
            /// <summary>الرابط يخص نطاق YouTube (رسمياً أو كنطاق مشابه/مزيف تم رفضه).</summary>
            public bool IsYouTubeHost { get; set; }
            /// <summary>تم استخراج معرّف فيديو صالح من نطاق رسمي.</summary>
            public bool IsValid { get; set; }
            public string VideoId { get; set; }
            public int StartSeconds { get; set; }
            /// <summary>رابط المشاهدة الموحد المخزن في قاعدة البيانات: https://www.youtube.com/watch?v=ID[&amp;t=Ns]</summary>
            public string WatchUrl { get; set; }
            /// <summary>رابط التضمين المستخدم عند العرض: https://www.youtube.com/embed/ID?...</summary>
            public string EmbedUrl { get; set; }
            public string Error { get; set; }
        }

        private const int MaxStartSeconds = 86400;

        private static readonly Regex VideoIdRegex = new Regex(@"^[A-Za-z0-9_-]{11}$", RegexOptions.Compiled);
        private static readonly Regex IframeSrcRegex = new Regex(@"src\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex SchemeRegex = new Regex(@"^[a-zA-Z][a-zA-Z0-9+.\-]*://", RegexOptions.Compiled);
        private static readonly Regex TimeRegex = new Regex(@"^(?:(\d+)h)?(?:(\d+)m)?(?:(\d+)s?)?$", RegexOptions.Compiled);
        private static readonly Regex FragmentTimeRegex = new Regex(@"[#&?]t=([^&]+)", RegexOptions.Compiled);

        private const string PlaylistError = "الرابط يشير إلى قائمة تشغيل كاملة وليس إلى فيديو محدد";
        private const string ChannelError = "الرابط يشير إلى قناة YouTube وليس إلى فيديو محدد";
        private const string NoIdError = "لم يتم العثور على معرّف فيديو YouTube صالح في الرابط";
        private const string FakeHostError = "نطاق الرابط ليس نطاق YouTube الرسمي (youtube.com أو youtu.be)";

        /// <summary>هل النطاق من نطاقات YouTube الرسمية؟</summary>
        public static bool IsOfficialYouTubeHost(string host)
        {
            if (string.IsNullOrWhiteSpace(host))
                return false;

            host = host.Trim().TrimEnd('.').ToLowerInvariant();
            return host == "youtube.com"
                || host.EndsWith(".youtube.com", StringComparison.Ordinal)
                || host == "youtube-nocookie.com"
                || host.EndsWith(".youtube-nocookie.com", StringComparison.Ordinal)
                || host == "youtu.be"
                || host == "www.youtu.be";
        }

        /// <summary>هل يبدو النطاق كأنه YouTube (يُستخدم لكشف النطاقات المزيفة مثل youtube.com.evil.io)؟</summary>
        public static bool LooksLikeYouTubeHost(string host)
        {
            if (string.IsNullOrWhiteSpace(host))
                return false;

            host = host.ToLowerInvariant();
            return host.Contains("youtube") || host.Contains("youtu.be");
        }

        public static string BuildWatchUrl(string videoId, int startSeconds)
        {
            return "https://www.youtube.com/watch?v=" + videoId + (startSeconds > 0 ? "&t=" + startSeconds + "s" : string.Empty);
        }

        public static string BuildEmbedUrl(string videoId, int startSeconds)
        {
            return "https://www.youtube.com/embed/" + videoId + "?rel=0&modestbranding=1&playsinline=1" + (startSeconds > 0 ? "&start=" + startSeconds : string.Empty);
        }

        /// <summary>يعيد رابط التضمين إذا كان الرابط المخزن رابط YouTube صالحاً، وإلا يعيد null.</summary>
        public static string GetEmbedUrl(string storedLink)
        {
            var info = Parse(storedLink);
            return info.IsValid ? info.EmbedUrl : null;
        }

        public static bool TryGetVideoId(string url, out string videoId)
        {
            var info = Parse(url);
            videoId = info.IsValid ? info.VideoId : null;
            return info.IsValid;
        }

        /// <summary>
        /// يحلل الرابط: إذا لم يكن من نطاق YouTube يعيد IsYouTubeHost=false (ويُترك للفحوصات العامة)،
        /// وإذا كان من YouTube يعيد المعرّف ورابطي المشاهدة والتضمين أو رسالة خطأ واضحة.
        /// </summary>
        public static YouTubeLinkInfo Parse(string input)
        {
            var info = new YouTubeLinkInfo();
            if (string.IsNullOrWhiteSpace(input))
            {
                info.Error = "الرابط فارغ";
                return info;
            }

            string url = input.Trim();

            // دعم لصق كود التضمين الكامل <iframe ... src="...">
            if (url.IndexOf("<iframe", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var m = IframeSrcRegex.Match(url);
                if (m.Success && !string.IsNullOrWhiteSpace(m.Groups[1].Value))
                {
                    url = m.Groups[1].Value.Trim();
                }
            }

            if (url.StartsWith("//", StringComparison.Ordinal))
            {
                url = "https:" + url;
            }

            // إضافة البروتوكول عند غيابه لروابط YouTube فقط (مثل youtu.be/ID أو www.youtube.com/watch?v=ID)
            if (!SchemeRegex.IsMatch(url))
            {
                string probeHost = url.Split('/', '?', '#')[0];
                if (!LooksLikeYouTubeHost(probeHost))
                {
                    return info;
                }
                url = "https://" + url;
            }

            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                if (LooksLikeYouTubeHost(url))
                {
                    info.IsYouTubeHost = true;
                    info.Error = "صيغة الرابط غير صالحة";
                }
                return info;
            }

            string host = (uri.Host ?? string.Empty).Trim().TrimEnd('.').ToLowerInvariant();
            if (!IsOfficialYouTubeHost(host))
            {
                if (LooksLikeYouTubeHost(host))
                {
                    info.IsYouTubeHost = true;
                    info.Error = FakeHostError;
                }
                return info;
            }

            info.IsYouTubeHost = true;

            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
            {
                info.Error = "بروتوكول الرابط غير مدعوم";
                return info;
            }

            var query = HttpUtility.ParseQueryString(uri.Query ?? string.Empty);
            string[] segments = uri.AbsolutePath.Trim('/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < segments.Length; i++)
            {
                segments[i] = Uri.UnescapeDataString(segments[i]);
            }

            string first = segments.Length > 0 ? segments[0].ToLowerInvariant() : string.Empty;
            string candidate = null;

            if (host == "youtu.be" || host == "www.youtu.be")
            {
                candidate = segments.Length > 0 ? segments[0] : null;
            }
            else if (first == "watch")
            {
                candidate = query["v"] ?? (segments.Length > 1 ? segments[1] : null);
            }
            else if (first == "shorts" || first == "live" || first == "embed" || first == "v" || first == "e")
            {
                candidate = segments.Length > 1 ? segments[1] : null;
            }
            else if (first == "attribution_link")
            {
                string inner = query["u"];
                if (!string.IsNullOrWhiteSpace(inner))
                {
                    var innerInfo = Parse("https://www.youtube.com" + (inner.StartsWith("/") ? string.Empty : "/") + inner);
                    if (innerInfo.IsValid)
                    {
                        return innerInfo;
                    }
                }
            }
            else if (first == "playlist")
            {
                info.Error = PlaylistError;
                return info;
            }
            else if (first.StartsWith("@") || first == "channel" || first == "c" || first == "user")
            {
                info.Error = ChannelError;
                return info;
            }
            else
            {
                candidate = query["v"];
            }

            if (string.Equals(candidate, "videoseries", StringComparison.OrdinalIgnoreCase))
            {
                info.Error = PlaylistError;
                return info;
            }

            if (string.IsNullOrWhiteSpace(candidate) || !VideoIdRegex.IsMatch(candidate))
            {
                info.Error = NoIdError;
                return info;
            }

            int start = ParseStartSeconds(query["t"]);
            if (start == 0) start = ParseStartSeconds(query["start"]);
            if (start == 0 && !string.IsNullOrEmpty(uri.Fragment))
            {
                var fm = FragmentTimeRegex.Match(uri.Fragment);
                if (fm.Success) start = ParseStartSeconds(fm.Groups[1].Value);
            }

            info.VideoId = candidate;
            info.StartSeconds = start;
            info.IsValid = true;
            info.WatchUrl = BuildWatchUrl(candidate, start);
            info.EmbedUrl = BuildEmbedUrl(candidate, start);
            return info;
        }

        /// <summary>يحوّل وقت البدء (90 أو 90s أو 1m30s أو 1h2m3s) إلى ثوانٍ، ويعيد 0 إذا كان غير صالح.</summary>
        public static int ParseStartSeconds(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            value = value.Trim().ToLowerInvariant();
            int plain;
            if (int.TryParse(value, out plain))
            {
                return plain > 0 ? Math.Min(plain, MaxStartSeconds) : 0;
            }

            var m = TimeRegex.Match(value);
            if (!m.Success || (!m.Groups[1].Success && !m.Groups[2].Success && !m.Groups[3].Success))
                return 0;

            long seconds = 0;
            if (m.Groups[1].Success) seconds += long.Parse(m.Groups[1].Value) * 3600;
            if (m.Groups[2].Success) seconds += long.Parse(m.Groups[2].Value) * 60;
            if (m.Groups[3].Success) seconds += long.Parse(m.Groups[3].Value);
            if (seconds <= 0) return 0;
            return (int)Math.Min(seconds, MaxStartSeconds);
        }
    }
}
