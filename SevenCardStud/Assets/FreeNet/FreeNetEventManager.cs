using System.Collections.Generic;
using FreeNet;

namespace FreeNetUnity
{
    public enum NETWORK_EVENT : byte
    {
        connected,
        disconnected,
        end
    }

    /// <summary>
    /// ネットワークフレームワークから発生したイベントをキューに保管
    /// ワーカースレッドとメインスレッド両方から呼び出される可能性があるので、スレッド同期化処理を行う。
    /// </summary>
    public class FreeNetEventManager
    {
        // 同期化オブジェクト
        object csEvent;

        // ネットワークフレームワークで発生したイベントを保持するキュー
        Queue<NETWORK_EVENT> networkEvents;

        // サーバーから受け取ったパケットを保持するキュー
        Queue<CPacket> networkMessageEvents;

        public FreeNetEventManager()
        {
            networkEvents = new Queue<NETWORK_EVENT>();
            networkMessageEvents = new Queue<CPacket>();
            csEvent = new object();
        }

        public void EnqueueNetworkEvent(NETWORK_EVENT eventType)
        {
            lock (csEvent)
            {
                networkEvents.Enqueue(eventType);
            }
        }

        public bool HasEvent()
        {
            lock (csEvent)
            {
                return networkEvents.Count > 0;
            }
        }

        public NETWORK_EVENT DequeueNetworkEvent()
        {
            lock (csEvent)
            {
                return networkEvents.Dequeue();
            }
        }

        public bool HasMessage()
        {
            lock (csEvent)
            {
                return networkMessageEvents.Count > 0;
            }
        }

        public void EnqueueNetworkMessage(CPacket buffer)
        {
            lock (csEvent)
            {
                networkMessageEvents.Enqueue(buffer);
            }
        }

        public CPacket DequeueNetworkMessage()
        {
            lock (csEvent)
            {
                return networkMessageEvents.Dequeue();
            }
        }
    }
}