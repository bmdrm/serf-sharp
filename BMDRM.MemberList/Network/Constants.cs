namespace BMDRM.MemberList.Network;

public static class Constants
{
    // This is the minimum and maximum protocol version that we can
    // _understand_. We're allowed to speak at any version within this
    // range. This range is inclusive.
    public const byte ProtocolVersionMin = 1;

    // Version 3 added support for TCP pings but we kept the default
    // protocol version at 2 to ease transition to this new feature.
    // A memberlist speaking version 2 of the protocol will attempt
    // to TCP ping another memberlist who understands version 3 or
    // greater.
    //
    // Version 4 added support for nacks as part of indirect probes.
    // A memberlist speaking version 2 of the protocol will expect
    // nacks from another memberlist who understands version 4 or
    // greater, and likewise nacks will be sent to memberlists who
    // understand version 4 or greater.
    public const byte ProtocolVersion2Compatible = 2;

    public const byte ProtocolVersionMax = 5;

    public const int MetaMaxSize = 512; // Maximum size for node meta data
    public const int CompoundHeaderOverhead = 2; // Assumed header overhead
    public const int CompoundOverhead = 2; // Assumed overhead per entry in compoundHeader
    public const int UserMsgOverhead = 1;
    public const int BlockingWarning = 10; // Warn if a UDP packet takes this long to process (in milliseconds)
    public const int MaxPushStateBytes = 20 * 1024 * 1024;
    public const int MaxPushPullRequests = 128; // Maximum number of concurrent push/pull requests
}
