using KeePassLib;

namespace KeePassSubsetExport.Tests.DataContainer
{
    public class TestKdfValues
    {
        public PwUuid KdfUuid { get; set; }
        public uint AesKeyTransformationRounds { get; set; }
        public uint Argon2Iterations { get; set; }
        public uint Argon2Memory { get; set; }
        public uint Argon2Parallelism { get; set; }

        public static readonly PwUuid UuidAes = new PwUuid(new byte[] {
            0xC9, 0xD9, 0xF3, 0x9A, 0x62, 0x8A, 0x44, 0x60,
            0xBF, 0x74, 0x0D, 0x08, 0xC1, 0x8A, 0x4F, 0xEA });

        public static readonly PwUuid UuidArgon2 = new PwUuid(new byte[] {
            0xEF, 0x63, 0x6D, 0xDF, 0x8C, 0x29, 0x44, 0x4B,
            0x91, 0xF7, 0xA9, 0xA4, 0x03, 0xE3, 0x0A, 0x0C });
    }
}
