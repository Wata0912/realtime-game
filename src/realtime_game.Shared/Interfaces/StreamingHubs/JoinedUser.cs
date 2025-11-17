using MessagePack;
using System;
using UnityEngine;
using realtime_game.Server.Models.Entities;
namespace Shared.Interfaces.StreamingHubs
{
    /// <summary>
    /// 入室済みユーザー
    /// </summary>
    [MessagePackObject]
    public class JoinedUser
    {
        [Key(0)]
        public Guid ConnectionId { get; set; }// 接続ID
        [Key(1)]
        public User UserData { get; set; }// ユーザー情報
        [Key(2)]
        public int JoinOrder { get; set; } // 参加順番
    }
}