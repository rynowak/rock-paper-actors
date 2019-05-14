using Rochambot;

namespace GameMaster
{
    public static class ScoringExtensions
    {
        public static SessionResult DetermineScore(this SessionResult result) =>
            (result.Player1.ShapeSelected, result.Player2.ShapeSelected) switch
        {
            (Shape.Paper, Shape.Rock) => result.PaperCoversRock(),
            (Shape.Paper, Shape.Scissors) => result.PaperCutByScissors(),
            (Shape.Rock, Shape.Paper) => result.RockCoveredByPaper(),
            (Shape.Rock, Shape.Scissors) => result.RockBreaksScissors(),
            (Shape.Scissors, Shape.Paper) => result.ScissorsCutPaper(),
            (Shape.Scissors, Shape.Rock) => result.ScissorsBrokenByRock(),
            (_, _) => result.TieGame(),
        };

        static SessionResult PaperCoversRock(this SessionResult result)
        {
            result.Summary = "You win! Paper covers rock.";
            result.Player1.IsWinner = true;
            result.Player2.IsWinner = false;
            return result;
        }

        static SessionResult PaperCutByScissors(this SessionResult result)
        {
            result.Summary = "You lose! Scissors cut paper.";
            result.Player1.IsWinner = false;
            result.Player2.IsWinner = true;
            return result;
        }
        static SessionResult RockCoveredByPaper(this SessionResult result)
        {
            result.Summary = "You lose! Paper covers rock.";
            result.Player1.IsWinner = false;
            result.Player2.IsWinner = true;
            return result;
        }
        static SessionResult RockBreaksScissors(this SessionResult result)
        {
            result.Summary = "You Win! Rock breaks scissors.";
            result.Player1.IsWinner = true;
            result.Player2.IsWinner = false;
            return result;
        }

        static SessionResult ScissorsCutPaper(this SessionResult result)
        {
            result.Summary = "You Win! Scissors cut paper.";
            result.Player1.IsWinner = true;
            result.Player2.IsWinner = false;
            return result;
        }

        static SessionResult ScissorsBrokenByRock(this SessionResult result)
        {
            result.Summary = "You lose. Rock breaks scissors.";
            result.Player1.IsWinner = false;
            result.Player2.IsWinner = true;
            return result;
        }

        static SessionResult TieGame(this SessionResult result)
        {
            result.Summary = "Tie";
            result.Player1.IsWinner = false;
            result.Player2.IsWinner = false;
            return result;
        }
    }
}