
namespace SevenPokerGameServer
{
    public enum PLAYER_STATE : short
    {
        // ルームに入場して準備が整ってない状態
        NOT_READY,

        // 準備ができた状態
        READY,

        // ディーラーからもらった4枚の中で1枚を捨てる。
        DISCARD_CARD,

        // 残りの3枚の中で1枚を表向きにする。
        OPEN_A_CARD,

        // ディーラーがカードを配る。
        DEAL_CARD,

        // チップをペッティング
        READY_FOR_BETTING_CHIPS,

        // フォールドの状態
        FOLD,

        // 観戦
        OBSERVE
    }
}