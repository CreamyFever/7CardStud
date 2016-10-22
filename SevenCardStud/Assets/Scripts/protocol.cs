namespace SevenPokerGameServer
{
    // サーバーと通信するためのプロトコルを定義。
    public enum PROTOCOL : short
    {
        BEGIN = 0,

        /// <summary>
        /// クライアント -> サーバー
        /// </summary>
        // ロビー入場を要求 Client -> Server(C -> S)
        ENTER_LOBBY_REQ,

        // ゲームルームを作成 C -> S
        CREATE_ROOM_REQ,

        // オートエントリー C -> S
        AUTO_ENTER_REQ,

        // ルームを出る C -> S
        EXIT_ROOM_REQ,

        // ルームの情報を要求 C -> S
        GAME_ROOM_INFO_REQ,

        // ゲームスタート C -> S
        GAME_START_REQ,

        // ディスカードパネルから捨てるカードを選択(手札4枚の中で1枚を) C -> S
        SELECT_DISCARD_CARD_REQ,

        SELECT_OPEN_CARD_REQ,

        // オープンーカードのパネルから残り3枚の中で1枚を表にする C -> S
        SHOW_A_CARD_ON_HAND_REQ,

        // カードが配られるように要求 C -> S
        DEAL_CARD_REQ,

        CURRENT_HAND_REQ,

        BETTING_REQ,


        /// <summary>
        /// サーバー -> クライアント
        /// </summary>
        // ロビー入場の要求に応答 Server -> Client(S -> C)
        ENTER_LOBBY_RES,

        // ゲームルーム作成に失敗したと応答 S -> C
        CREATE_ROOM_NO,

        // 入れるゲームルームがないと知らせる S -> C
        ENTER_ROOM_NO,

        // 入れるゲームルームがないと知らせる S -> C
        AUTO_ENTER_NO,

        // ゲームルームのシーンに切り替える S -> C
        ENTER_ROOM_OK,

        // ルームを出る S -> C
        EXIT_ROOM_OK,

        // 他のプレイヤーが入場したと知らせる  S -> C
        PLAYER_ENTER_ROOM,

        // 他のプレイヤーがルームを出たと知らせる S -> C
        PLAYER_EXIT_ROOM,

        // ルームの情報要求に応答 S -> C
        GAME_ROOM_INFO_RES,

        // ゲームを始める S -> C
        GAME_START_RES,

        // ゲームスタートの後、4枚のカードを配ると応答 S -> C
        DEAL_FIRST_CARD_RES,

        // 捨てるカードを選択
        SELECT_DISCARD_CARD_RES,

        // 他のプレイヤーにカードを捨てたとブロードキャスト
        BROADCAST_DISCARD_CARD,

        BROADCAST_OPEN_CARD,

        SHOW_A_CARD_ON_HAND_RES,

        // 役を判定した結果を各クライアントに送信 S -> C
        EVALUATE_COMMUNITY_CARDS,

        DEAL_CARD_RES,

        READY_TO_BET,

        CURRENT_HAND,

        BETTING_RES,

        SHOW_RESULT,

        END
    }
}