using System;
using System.Linq;
using System.Threading;
using SMG_Test.Data;
using SMG_Test.Data.Models;

namespace SMG_Test
{
  public class Game
  {
    private static Random _rand = new Random();
    private readonly IDisplayer _displayer;
    private readonly IReader _reader;
    private readonly IGameContext _context;

    public Game(IDisplayer displayer, IReader reader, IGameContext context)
    {
      _displayer = displayer ?? throw new ArgumentNullException(nameof(displayer));
      _reader = reader ?? throw new ArgumentNullException(nameof(reader));
      _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Run()
    {
      _displayer.PrintLine("---Rock Paper Scissors---");

      bool continueGame = true;
      while (continueGame)
      {
        PlayRound();

        _displayer.PrintLine();
        _displayer.PrintLine("Would you like to play again? (y/n)");
        continueGame = _reader.ReadLine().ToLowerInvariant()[0] == 'y';
      }

      PrintScores();
    }

    private void PlayRound()
    {
      Move playerMove;
      Move computerMove;
      bool playerGoesFirst = SelectFirstPlayer() == PlayerType.Human;

      if (playerGoesFirst)
      {
        _displayer.PrintLine();
        _displayer.PrintLine("Player goes first!");

        playerMove = GetPlayerMove();
        _displayer.PrintLine();
        _displayer.PrintLine($"Player chose {playerMove}");

        computerMove = GetComputerMove();
        _displayer.PrintLine($"Computer chose {computerMove}");
        _displayer.PrintLine();

      }
      else
      {
        _displayer.PrintLine();
        _displayer.PrintLine("Computer goes first!");

        computerMove = GetComputerMove();
        _displayer.PrintLine("Computer is deciding...");
        _displayer.PrintLine();
        Thread.Sleep(250);

        playerMove = GetPlayerMove();
        _displayer.PrintLine();
        _displayer.PrintLine($"Player chose {playerMove}");
        _displayer.PrintLine($"Computer chose {computerMove}");
        _displayer.PrintLine();
      }

      // decide round winner
      GameResult result = CalculateResult(playerMove, computerMove);

      // save round to db
      var record = new Round
      {
        PlayerMove = playerMove,
        ComputerMove = computerMove,
        GameResult = result
      };
      _context.SaveResult(record);

      if (result == GameResult.Tie)
        _displayer.PrintLine("Tie!");
      else if (result == GameResult.PlayerWin)
        _displayer.PrintLine("Player wins!");
      else
        _displayer.PrintLine("Computer wins!");
    }

    private PlayerType SelectFirstPlayer()
    {
      return _rand.NextDouble() < 0.5 ? PlayerType.Human : PlayerType.Computer;
    }

    private Move GetPlayerMove()
    {
      _displayer.PrintLine("Player, choose your move:");
      _displayer.PrintLine();

      int playerInput;

      PrintMoveOptions();
      while (!int.TryParse(_reader.ReadLine(), out playerInput) || !IsValidMove(playerInput))
      {
        _displayer.PrintLine();
        _displayer.PrintLine("That was not a valid move. Please try again!");
        PrintMoveOptions();
      }

      return (Move)(playerInput - 1);
    }

    private Move GetComputerMove()
    {
      return (Move)_rand.Next(0, 3);
    }

    private GameResult CalculateResult(Move playerMove, Move computerMove)
    {
      if (playerMove == computerMove)
        return GameResult.Tie;

      // test all possible player wins
      if (playerMove == Move.Rock && computerMove == Move.Scissors)
      {
        return GameResult.PlayerWin;
      }
      if (playerMove == Move.Paper && computerMove == Move.Rock)
      {
        return GameResult.PlayerWin;
      }
      if (playerMove == Move.Scissors && computerMove == Move.Paper)
      {
        return GameResult.PlayerWin;
      }

      // neither a tie nor a player win implies the computer must have won
      return GameResult.PlayerLose;
    }

    private void PrintMoveOptions()
    {
      string composite = "{0,-20} ({1})";
      _displayer.PrintLineFormat(composite, "Rock", 1);
      _displayer.PrintLineFormat(composite, "Paper", 2);
      _displayer.PrintLineFormat(composite, "Scissors", 3);
      _displayer.PrintLine();
    }

    private void PrintScores()
    {
      var stats = _context.SessionStats;

      _displayer.PrintLine();
      _displayer.PrintLine("SESSION OVER");
      _displayer.PrintLine("Generating match report...");
      Thread.Sleep(250);
      _displayer.PrintLine();

      if (stats.TotalPlayerWins == stats.TotalPlayerLosses)
        _displayer.PrintLine("TIE!");
      else if (stats.TotalPlayerWins > stats.TotalPlayerLosses)
        _displayer.PrintLine("PLAYER WINS!");
      else
        _displayer.PrintLine("COMPUTER WINS!");

      _displayer.PrintLine();

      new StatsReporter(_displayer).PrintReport(stats);
    }

    private bool IsValidMove(int moveVal)
    {
      return new int[] { 1, 2, 3 }.Contains(moveVal);
    }

  }
}