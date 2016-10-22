using System;
using System.Collections.Generic;

namespace SevenPokerGameServer
{
    // ゲームルームを管理するマネジャークラス
    public class CGameRoomManager
    {
        List<CGameRoom> m_Rooms;

        Dictionary<int, GAME_STATE> m_GameRoomState;         // ルームの状態を管理 <ルームナンバー、ルームの状態>

        public CGameRoomManager()
        {
            m_Rooms = new List<CGameRoom>();

            m_GameRoomState = new Dictionary<int, GAME_STATE>();
        }

        /// <summary>
        /// ルーム作成を要求したユーザーを受け取り、ゲームルームを生成。
        /// </summary>
        /// <param name = "user"></param>
        public void CreateRoom(CGameUser user, int roomIndex, long stake)
        {
            Console.WriteLine("Created Room Index : " + roomIndex);
            // ゲームルームを作成して入場
            CGameRoom newRoom = new CGameRoom(roomIndex, user, stake);

            // ルームのリストに新しく出来たルームを追加
            m_Rooms.Add(newRoom);
            ChangeRoomState(newRoom, GAME_STATE.WAITING);
        }

        /// <summary>
        /// 空席があるルームに自動的に入場
        /// </summary>
        /// <param name="user"></param>
        public bool EnterRoomAutomatically(CGameUser user, long ante)
        {
            for (int i = 0; i < m_Rooms.Count; ++i)
            {
                if (!m_Rooms[i].isFull() && m_Rooms[i].m_Ante == ante)
                {
                    Console.WriteLine("User entered into Room " + m_Rooms[i].m_RoomIndex + ".");
                    m_Rooms[i].EnterGameRoom(user);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// ルームを出る際に呼び出す。
        /// </summary>
        /// <param name="user"></param>
        public void ExitRoom(CGameUser user)
        {
            // ロビーに入ってからルームを一度も作らずにデバイスを終了した場合による例外処理。
            if (user.gameRoom != null)
            {
                user.gameRoom.ExitGameRoom(user);

                // ルームにプレイヤーがないならルームを削除
                if (user.gameRoom.GetPlayersCount() < 1)
                {
                    m_GameRoomState.Remove(user.gameRoom.m_RoomIndex);
                    RemoveRoom(user.gameRoom);
                }

                user.ExitRoom();
            }
        }


        /// <summary>
        /// ルーム削除の処理
        /// </summary>
        /// <param name="room"></param>
        public void RemoveRoom(CGameRoom room)
        {
            m_Rooms.Remove(room);
        }

        /// <summary>
        /// ルームのインデックスを取得
        /// </summary>
        /// <returns></returns>
        public List<int> GetRoomsIndex()
        {
            List<int> indexes = new List<int>();

            for (int i = 0; i < m_Rooms.Count; ++i)
            {
                indexes.Add(m_Rooms[i].m_RoomIndex);
            }

            return indexes;
        }

        public bool isGameRoomState(int roomIndex, GAME_STATE cState)
        {
            if (m_GameRoomState[roomIndex] == cState)
                return true;
            else
                return false;
        }

        /// <summary>
        /// ルームの状態を変更させるメソッド
        /// </summary>
        /// <param name="room"></param>
        /// <param name="state"></param>
        public void ChangeRoomState(CGameRoom room, GAME_STATE state)
        {
            if (m_GameRoomState.ContainsKey(room.m_RoomIndex))
            {
                m_GameRoomState[room.m_RoomIndex] = state;
            }
            else
            {
                m_GameRoomState.Add(room.m_RoomIndex, state);
            }
        }
    }
}