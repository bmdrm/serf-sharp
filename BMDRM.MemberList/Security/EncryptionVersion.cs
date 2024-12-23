namespace BMDRM.MemberList.Security
{
    /// <summary>
    /// Encrypted messages are prefixed with an encryptionVersion byte
    /// that is used for us to be able to properly encode/decode. We
    /// currently support the following versions:
    /// 
    /// 0 - AES-GCM 128, using PKCS7 padding
    /// 1 - AES-GCM 128, no padding. Padding not needed, caused bloat.
    /// </summary>
    public enum EncryptionVersion : byte
    {
        MinVersion = 0,
        MaxVersion = 1
    }
}
