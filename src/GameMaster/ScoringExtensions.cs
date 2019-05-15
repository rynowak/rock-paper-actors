using Rochambot;

namespace GameMaster
{
    public static class ScoringExtensions
    {
        public static Turn DetermineScore(this Turn result) =>
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

        static Turn PaperCoversRock(this Turn turn)
        {
            turn.Summary = "You win! Paper covers rock.";
            turn.Player1.IsWinner = true;
            turn.Player2.IsWinner = false;
            return turn;
        }

        static Turn PaperCutByScissors(this Turn turn)
        {
            turn.Summary = "You lose! Scissors cut paper.";
            turn.Player1.IsWinner = false;
            turn.Player2.IsWinner = true;
            return turn;
        }
        static Turn RockCoveredByPaper(this Turn turn)
        {
            turn.Summary = "You lose! Paper covers rock.";
            turn.Player1.IsWinner = false;
            turn.Player2.IsWinner = true;
            return turn;
        }
        static Turn RockBreaksScissors(this Turn turn)
        {
            turn.Summary = "You Win! Rock breaks scissors.";
            turn.Player1.IsWinner = true;
            turn.Player2.IsWinner = false;
            return turn;
        }

        static Turn ScissorsCutPaper(this Turn turn)
        {
            turn.Summary = "You Win! Scissors cut paper.";
            turn.Player1.IsWinner = true;
            turn.Player2.IsWinner = false;
            return turn;
        }

        static Turn ScissorsBrokenByRock(this Turn turn)
        {
            turn.Summary = "You lose. Rock breaks scissors.";
            turn.Player1.IsWinner = false;
            turn.Player2.IsWinner = true;
            return turn;
        }

        static Turn TieGame(this Turn turn)
        {
            turn.Summary = "Tie";
            turn.Player1.IsWinner = false;
            turn.Player2.IsWinner = false;
            return turn;
        }
    }
}