
namespace SevenPokerGameServer
{
    // カードのクラス(スート、ランク)

    public enum CARD_SUIT : int
    {
        CLOVER = 0,
        HEART = 1,
        DIAMOND = 2,
        SPADE = 3
    }

    public enum CARD_RANK : int
    {
        Two = 2,
        Three,
        Four,
        Five,
        Six,
        Seven,
        Eight,
        Nine,
        Ten,
        Jack,
        Queen,
        King,
        Ace
    }

    public class CCard
    {
        int m_Suit;
        int m_Rank;

        public int Suit
        {
            get { return m_Suit; }
            private set { m_Suit = value; }
        }

        public int Rank
        {
            get { return m_Rank; }
            private set { m_Rank = value; }
        }

        public CCard()
        {

        }

        public CCard(CARD_SUIT _suit, CARD_RANK _rank)
        {
            m_Suit = (int)_suit;
            m_Rank = (int)_rank;
        }

        public string GetString()
        {
            string msg = "";

            switch ((CARD_SUIT)m_Suit)
            {
                case CARD_SUIT.CLOVER:
                    msg += "CLOVER ";
                    break;

                case CARD_SUIT.HEART:
                    msg += "HEART ";
                    break;

                case CARD_SUIT.DIAMOND:
                    msg += "DIAMOND ";
                    break;

                case CARD_SUIT.SPADE:
                    msg += "SPADE ";
                    break;
            }

            switch ((CARD_RANK)m_Rank)
            {
                case CARD_RANK.Jack:
                    msg += "JACK";
                    break;

                case CARD_RANK.Queen:
                    msg += "QUEEN";
                    break;

                case CARD_RANK.King:
                    msg += "KING";
                    break;

                case CARD_RANK.Ace:
                    msg += "ACE";
                    break;

                default:
                    msg += m_Rank.ToString();
                    break;
            }

            return msg;
        }
    }
}