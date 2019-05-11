namespace GameMaster
{
    public static class ScoringExtensions
    {
        public static SessionResult Score(this SessionResult sessionResult)
        {
            if(sessionResult.Player1.ShapeSelected.Equals(Shape.Paper))
            {
                if(sessionResult.Player2.ShapeSelected.Equals(Shape.Paper))
                {
                    sessionResult.Summary = "Tie";
                    sessionResult.Player1.IsWinner = false;
                    sessionResult.Player2.IsWinner = false;
                }
                else if(sessionResult.Player2.ShapeSelected.Equals(Shape.Rock))
                {
                    sessionResult.Summary = "You Win! Paper covers rock.";
                    sessionResult.Player1.IsWinner = true;
                    sessionResult.Player2.IsWinner = false;
                }
                else if(sessionResult.Player2.ShapeSelected.Equals(Shape.Scissors))
                {
                    sessionResult.Summary = "You lose. Scissors cut paper.";
                    sessionResult.Player1.IsWinner = false;
                    sessionResult.Player2.IsWinner = true;
                } 
            }
            else if(sessionResult.Player1.ShapeSelected.Equals(Shape.Rock))
            {
                if(sessionResult.Player2.ShapeSelected.Equals(Shape.Paper))
                {
                    sessionResult.Summary = "You lose. Paper covers rock.";
                    sessionResult.Player1.IsWinner = false;
                    sessionResult.Player2.IsWinner = true;
                }
                else if(sessionResult.Player2.ShapeSelected.Equals(Shape.Rock))
                {
                    sessionResult.Summary = "Tie";
                    sessionResult.Player1.IsWinner = false;
                    sessionResult.Player2.IsWinner = false;
                }
                else if(sessionResult.Player2.ShapeSelected.Equals(Shape.Scissors))
                {
                    sessionResult.Summary = "You Win! Rock breaks scissors.";
                    sessionResult.Player1.IsWinner = true;
                    sessionResult.Player2.IsWinner = false;
                } 
            }
            else if(sessionResult.Player1.ShapeSelected.Equals(Shape.Scissors))
            {
                if(sessionResult.Player2.ShapeSelected.Equals(Shape.Paper))
                {
                    sessionResult.Summary = "You Win! Scissors cut paper.";
                    sessionResult.Player1.IsWinner = true;
                    sessionResult.Player2.IsWinner = false;
                }
                else if(sessionResult.Player2.ShapeSelected.Equals(Shape.Rock))
                {
                    sessionResult.Summary = "You lose. Rock breaks scissors.";
                    sessionResult.Player1.IsWinner = false;
                    sessionResult.Player2.IsWinner = true;
                }
                else if(sessionResult.Player2.ShapeSelected.Equals(Shape.Scissors))
                {
                    sessionResult.Summary = "Tie";
                    sessionResult.Player1.IsWinner = false;
                    sessionResult.Player2.IsWinner = false;
                } 
            } 
            return sessionResult;
        }
    }
}