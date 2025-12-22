using System;
using UnityEngine;

public class RoomUser : MonoBehaviour
{
    public Guid ConnectionId;
    public int userId;
    public string userName;
    

    // 今操作しているベイ（死んだら null）
    public PlayerTop bay;

   
}
