using System;
using FreeNet;

namespace FreeNetUnity
{
    public class CRemoteServerPeer : IPeer
    {
        public CUserToken token { get; private set; }
        WeakReference freeNetEventmanager;

        public CRemoteServerPeer(CUserToken _token)
        {
            token = _token;
            token.SetPeer(this);
        }

        public void SetEventManager(FreeNetEventManager eventManager)
        {
            freeNetEventmanager = new WeakReference(eventManager);
        }

        /// <summary>
        /// メッセージを受信した場合に呼び出される
        /// パラメーターとして受け取ったバッファはワーカースレッドで再利用するのでコピーしてアプリケーションに渡す。
        /// </summary>
        /// <param name="buffer"></param>
        void IPeer.onMessage(Const<byte[]> buffer)
        {
            // パッファをコピーした後、CPacketに包んでから渡す。
            byte[] appBuffer = new byte[buffer.Value.Length];
            Array.Copy(buffer.Value, appBuffer, buffer.Value.Length);
            CPacket msg = new CPacket(appBuffer, this);
            (freeNetEventmanager.Target as FreeNetEventManager).EnqueueNetworkMessage(msg);
        }

        void IPeer.onRemoved()
        {
            (freeNetEventmanager.Target as FreeNetEventManager).EnqueueNetworkEvent(NETWORK_EVENT.disconnected);
        }

        void IPeer.Send(CPacket msg)
        {
            token.Send(msg);
        }

        void IPeer.Disconnect()
        {
        }

        void IPeer.ProcessUserOperation(CPacket msg)
        {
        }
    }
}