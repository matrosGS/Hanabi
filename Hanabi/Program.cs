
using System.Collections.Generic;
using System.Linq;
using System;

namespace Hanabi {

    enum Color {
        Red,
        Green,
        Blue,
        White,
        Yellow
    };

    static class ColorMethods {

        public static Color GetColor(string color) {
            switch (color) {
                case "Red":
                    return Color.Red;
                case "Green":
                    return Color.Green;
                case "Blue":
                    return Color.Blue;
                case "White":
                    return Color.White;
                case "Yellow":
                    return Color.Yellow;
                default:
                    throw new Exception("Wrong color");
            }
        }
    }

    enum Command {
        StartGame,
        PlayCard,
        DropCard,
        TellColor,
        TellRank
    };

    static class CommandMethods {
        public static Command DetermineCommand(string input) {
            var starter = string.Join(" ", input.Split(' ').Take(2));
            switch (starter) {
                case "Start new":
                    return Command.StartGame;
                case "Play card":
                    return Command.PlayCard;
                case "Drop card":
                    return Command.DropCard;
                case "Tell color":
                    return Command.TellColor;
                case "Tell rank":
                    return Command.TellRank;
                default:
                    throw new Exception("Wrong command");
            }
        }

        public static int CommandLength(Command command) {
            var gameStarter = "Start new game with deck ";
            var playCard = "Play card ";
            var dropCard = "Drop card ";
            var tellColor = "Tell color ";
            var tellRank = "Tell rank ";
            switch (command) {
                case Command.StartGame:
                    return gameStarter.Length;
                case Command.PlayCard:
                    return playCard.Length;
                case Command.DropCard:
                    return dropCard.Length;
                case Command.TellColor:
                    return tellColor.Length;
                case Command.TellRank:
                    return tellRank.Length;
                default:
                    throw new Exception("Wrong command");
            }
        }
    }

    class Program {

        public static void Main() {
            string input;
            Game game = null;

            do {
                input = Console.ReadLine();
                if (input == null) {
                    break;
                }

                if (CommandMethods.DetermineCommand(input) == Command.StartGame) {
                    input = input.Substring(CommandMethods.CommandLength(Command.StartGame));
                    game = new Game(input);
                }
                else {
                    game.StartTurn();
                    game.ProcessTurn(input);

                    if (game.IsFinished) {
                        game.EndGame();
                    }
                    else {
                        game.EndTurn();
                    }
                }
            } while (input != null);
        }
    }

    class Game {

        public const int PlayersCount = 2;
        public const int ColorsCount = 5;

        private Queue<Card> deck;
        private int[] table;

        public Player[] Players { get; set; }
        public int CurrentPlayer { get; set; }

        public int Turn { get; set; }
        public int CardsPlayed { get; set; }
        public int WithRisk { get; set; }

        private bool finished;
        public bool IsFinished {
            get {
                return finished
                    || deck.Count() == 0
                    || CardsPlayed == 25
                    || Players.FirstOrDefault(x => x.MadeForbiddenMove == true) != null;
            }

            set {
                finished = value;
            }
        }

        public Game(string cards) {
            InitializeGame(cards);
        }

        private void InitializeGame(string cards) {
            table = new int[ColorsCount];

            Players = new Player[PlayersCount];
            Players[0] = new Player(cards.Substring(0, (Player.HandSize * 3) - 1));
            Players[1] = new Player(cards.Substring((Player.HandSize * 3), (Player.HandSize * 3)));
            cards = cards.Substring(Player.HandSize * 3 * PlayersCount);
            deck = new Queue<Card>();
            cards
                .Split(' ')
                .ToList()
                .ForEach(card => deck.Enqueue(new Card(card)));
        }

        public void StartTurn() {
            Turn++;
        }

        public void ProcessTurn(string input) {
            int position;
            int value;
            int[] positions;
            switch (CommandMethods.DetermineCommand(input)) {
                case Command.PlayCard:
                    position = int.Parse(input.Substring(input.Length - 2));
                    Players[CurrentPlayer].MadeForbiddenMove = PlayCard(position);
                    break;
                case Command.DropCard:
                    position = int.Parse(input.Substring(input.Length - 2));
                    DropCard(position);
                    break;
                case Command.TellColor:
                    input = input.Substring(CommandMethods.CommandLength(Command.TellColor));
                    value = (int)ColorMethods.GetColor(input.Substring(0, input.IndexOf(" ")));
                    positions = ParseCommandArguments(input, value);

                    Players[CurrentPlayer].MadeForbiddenMove = TellColor((Color)value, positions);
                    break;
                case Command.TellRank:
                    input = input.Substring(CommandMethods.CommandLength(Command.TellRank));
                    value = int.Parse(input.Substring(0, 1));
                    positions = ParseCommandArguments(input, value);

                    Players[CurrentPlayer].MadeForbiddenMove = TellRank(value, positions);
                    break;
            }
        }

        private int[] ParseCommandArguments(string input, int value) {
            input = input.Substring(input.IndexOf("cards ") + 6);
            var positions = input
                .Split(' ')
                .ToList()
                .Select(arg => int.Parse(arg))
                .ToArray();
            return positions;
        }

        public void EndTurn() {
            CurrentPlayer = GetOtherPlayerID();
        }

        public void EndGame() {
            PrintEndGameInfo();
        }

        public void PrintEndGameInfo() {
            Console.WriteLine("Turn: {0}, cards: {1}, with risk: {2}", Turn, CardsPlayed, WithRisk);
        }

        public int GetOtherPlayerID() {
            return (CurrentPlayer + 1) % PlayersCount;
        }

        public void DropCard(int number) {
            List<Card> currentHand = Players[CurrentPlayer].Hand;
            ChangeCard(number, currentHand);
        }

        public bool PlayCard(int number) {
            List<Card> currentHand = Players[CurrentPlayer].Hand;
            Card playedCard = currentHand.ElementAt(number);
            ChangeCard(number, currentHand);

            var wrongCard = playedCard.Rank - 1 != table[(int)playedCard.Color];

            if (wrongCard) {
                return true;
            }
            else {
                if (IsRiskyPlay(playedCard)) {
                    WithRisk++;
                }

                table[(int)playedCard.Color]++;
                CardsPlayed++;
                return false;
            }
        }

        private void ChangeCard(int number, List<Card> hand) {
            hand.RemoveAt(number);
            hand.Add(deck.Dequeue());
        }

        private bool IsRiskyPlay(Card playedCard) {
            if (playedCard.IsColorKnown && playedCard.IsRankKnown) {
                return false;
            } 

            var sameRanksOnTable = table
                .ToList()
                .All(value => value == table.First());
            var possibleColorsSame = playedCard.PossibleColors
                .Select(color => table[(int)color])
                .Distinct()
                .Count() == 1;
            if ((possibleColorsSame || sameRanksOnTable) && playedCard.IsRankKnown) {
                return false;
            } 

            return true;
        }

        public bool TellColor(Color color, params int[] args) {
            int otherPlayerID = GetOtherPlayerID();
            List<Card> otherPlayersHand = Players[otherPlayerID].Hand;

            args.ToList().ForEach(position => {
                otherPlayersHand.ElementAt(position).IsColorKnown = true;
                otherPlayersHand.ElementAt(position).PossibleColors.Clear();
                otherPlayersHand.ElementAt(position).PossibleColors.Add(color);
            });

            new int[] { 0, 1, 2, 3, 4 }.Except(args)
                .ToList()
                .ForEach(position => otherPlayersHand[position].PossibleColors.Remove(color));

            return IsColorInfoCorrect(color, otherPlayersHand, args);
        }

        public bool TellRank(int rank, params int[] args) {
            int otherPlayerID = GetOtherPlayerID();
            List<Card> otherPlayersHand = Players[otherPlayerID].Hand;

            args.ToList().ForEach(position => {
                otherPlayersHand.ElementAt(position).IsRankKnown = true;
                otherPlayersHand.ElementAt(position).PossibleRanks.Clear();
                otherPlayersHand.ElementAt(position).PossibleRanks.Add(rank);
            });

            new int[] { 0, 1, 2, 3, 4 }.Except(args)
                .ToList()
                .ForEach(position => otherPlayersHand[position].PossibleRanks.Remove(rank));

            return IsRankInfoCorrect(rank, otherPlayersHand, args);
        }

        public bool IsRankInfoCorrect(int rank, List<Card> hand, int[] args) {
            var correct = args
                .ToList()
                .Where(x => hand.ElementAt(x).Rank != rank)
                .Count() != 0;

            var full = hand
                .Where(x => x.Rank == rank)
                .Count() != args.Count();

            return correct || full;
        }

        public bool IsColorInfoCorrect(Color color, List<Card> hand, int[] args) {
            var correct = args
                .ToList()
                .Where(x => hand.ElementAt(x).Color != color)
                .Count() != 0;

            var full = hand
                .Where(x => x.Color == color)
                .Count() != args.Count();

            return correct || full;
        }
    }

    class Player {

        public const int HandSize = 5;

        public List<Card> Hand { get; set; }
        public bool MadeForbiddenMove { get; set; }

        public Player(string inputCards) {
            MadeForbiddenMove = false;
            Hand = new List<Card>();
            string[] cards = inputCards.Trim().Split(' ');
            cards
                .ToList()
                .ForEach(card => Hand.Add(new Card(card)));
        }
    }

    class Card {

        public string Value { get; set; }

        public Color Color {

            get {
                string val = this.Value.Substring(0, 1);
                switch (val) {
                    case "R":
                        return Color.Red;
                    case "G":
                        return Color.Green;
                    case "B":
                        return Color.Blue;
                    case "W":
                        return Color.White;
                    case "Y":
                        return Color.Yellow;
                    default:
                        throw new Exception("Wrong card value");
                }
            }

        }
        public int Rank {

            get {
                return int.Parse(Value.Trim().Substring(1));
            }
        }
        public List<Color> PossibleColors { get; set; }
        public List<int> PossibleRanks { get; set; }

        private bool rankKnown;
        public bool IsRankKnown {
            get {
                return rankKnown || PossibleRanks.Count == 1;
            }

            set {
                rankKnown = value;
            }
        }

        private bool colorKnown;
        public bool IsColorKnown {
            get {
                return colorKnown || PossibleColors.Count == 1;
            }

            set {
                colorKnown = value;
            }
        }

        public Card(string value) {
            Value = value;
            IsColorKnown = false;
            IsRankKnown = false;
            PossibleColors = Enum.GetValues(typeof(Color)).OfType<Color>().ToList();
            PossibleRanks = new List<int>(new int[] { 1, 2, 3, 4, 5 });
        }
    }
}
