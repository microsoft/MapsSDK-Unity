// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class NetSyncConnection
  {
    public readonly long ConnectionId;
    /// If status is Disconnected, specifies the reason.
    public readonly NetSyncDisconnectReason DisconnectReason;
    /// The ID of the local session. Will be null if the connection is not active
    public readonly UInt64 SessionId;
    public readonly NetSyncConnectionStatus Status;
    public readonly string ZoneId;


    public NetSyncConnection(IntPtr o)
    {
      ConnectionId = CAPI.ovr_NetSyncConnection_GetConnectionId(o);
      DisconnectReason = CAPI.ovr_NetSyncConnection_GetDisconnectReason(o);
      SessionId = CAPI.ovr_NetSyncConnection_GetSessionId(o);
      Status = CAPI.ovr_NetSyncConnection_GetStatus(o);
      ZoneId = CAPI.ovr_NetSyncConnection_GetZoneId(o);
    }
  }

}
