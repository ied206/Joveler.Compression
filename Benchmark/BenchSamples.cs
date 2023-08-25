using System.Collections.Generic;

namespace Benchmark
{
    public class BenchSamples
    {
        public static List<string> SampleFileNames { get; set; } = new List<string>()
        {
            "Banner.bmp", // From PEBakery EncodedFile tests
            "Banner.svg", // From PEBakery EncodedFile tests
            "bible_en_utf8.txt", // From Canterbury Corpus
            "bible_kr_cp949.txt", // Public Domain (개역한글)
            "bible_kr_utf8.txt", // Public Domain (개역한글)
            "bible_kr_utf16le.txt", // Public Domain (개역한글)
            "ooffice.dll", // From Silesia corpus
            "reymont.pdf", // From Silesia corpus
            "world192.txt", // From Canterbury corpus
        };

        public static List<string> LessFileNames { get; set; } = new List<string>()
        {
            "bible_en_utf8.txt", // From Canterbury Corpus
            "bible_kr_utf8.txt", // Public Domain (개역한글)
            "ooffice.dll", // From Silesia corpus
            "reymont.pdf", // From Silesia corpus
            "world192.txt", // From Canterbury corpus
        };

        public static List<string> Levels { get; set; } = new List<string>()
        {
            "Fastest",
            "Default",
            "Best",
        };
}
}
