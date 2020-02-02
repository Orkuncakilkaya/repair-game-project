using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using NDream.AirConsole;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Random = System.Random;

public class Air : MonoBehaviour
{
    public int tourSeconds = 2;
    public int maxButtons = 1;
    public int maxSteps = 20;
    private readonly List<int> _playerDeviceIdList = new List<int>();
    private readonly List<PlayerTour> _playerTours = new List<PlayerTour>();
    private static readonly List<int> _readyPlayerList = new List<int>();
    private static readonly Dictionary<int, PadState> PlayerPadState = new Dictionary<int, PadState>();
    private int _currentTour = 0;
    private int lastSecond = 0;
    private int secondsPassed = 0;
    private float totalPercentage = 0;
    private bool gameStarted = false;
    private Parent _parent;
    private bool startFired = false;

    // Start is called before the first frame update
    private void Start()
    {
        AirConsole.instance.onMessage += OnMessage;
        AirConsole.instance.onConnect += OnConnect;
        AirConsole.instance.onDisconnect += OnDisconnect;
    }

    private void prepareGame()
    {
        foreach (var player in _readyPlayerList)
        {
            AirConsole.instance.Message(player, new {type = "state", state = "Game"});
        }

        GameObject gameObject = GameObject.FindWithTag("Parent");
        this._parent = gameObject.GetComponent<Parent>();
        _parent.kaboom = true;

        this.createLevel();
        Invoke(nameof(startGame), 5);
    }

    private void startGame()
    {
        this._parent.hasMagic = true;
        this.gameStarted = true;
        this.startTour();
    }

    private void createLevel()
    {
        Random random = new Random();
        foreach (var player in _playerDeviceIdList)
        {
            for (int i = 0; i <= maxSteps; i++)
            {
                List<int> rands = new List<int>();
                for (int j = 1; j <= 4; j++)
                {
                    if (rands.Sum(t => Convert.ToInt32(t)) >= maxButtons * 100)
                    {
                        rands.Add(0);
                    }
                    else
                    {
                        rands.Add(random.Next(0, 100));
                    }
                }

                ButtonState left = rands[0] > 50 ? ButtonState.Pressed : ButtonState.Released;
                ButtonState right = rands[1] > 50 ? ButtonState.Pressed : ButtonState.Released;
                ButtonState up = rands[2] > 50 ? ButtonState.Pressed : ButtonState.Released;
                ButtonState down = rands[3] > 50 ? ButtonState.Pressed : ButtonState.Released;

                PadState target = new PadState(left, right, up, down);

                PlayerTour tour = new PlayerTour(i, player, target, false);
                this._playerTours.Add(tour);
            }
        }
    }

    private void Update()
    {
        if (!gameStarted)
        {
            if (_readyPlayerList.Count == _playerDeviceIdList.Count && _readyPlayerList.Count > 0 && !startFired)
            {
                Invoke(nameof(prepareGame), 3);
                startFired = true;
            }

            return;
        }

        int seconds = (int) Time.timeSinceLevelLoad % 60;
        if (this.lastSecond != seconds)
        {
            this.secondsPassed++;
            this.tourTick();
            this.lastSecond = seconds;
        }

        var tour = _playerTours.FirstOrDefault(t => t.Tour == this._currentTour);
        if (tour != null)
        {
            if (tour.Target.CompareTo(PlayerPadState[_playerDeviceIdList[0]]))
            {
                tour.Successful = true;
                this.nextTour();
            }
        }
    }

    private PadState getPadStateOfPlayer(int id)
    {
        return PlayerPadState.ContainsKey(id) ? PlayerPadState[id] : new PadState();
    }

    private void tourTick()
    {
        if (this.secondsPassed >= this.tourSeconds)
        {
            this.nextTour();
        }
    }

    private void startTour()
    {
        List<PlayerTour> tourList = _playerTours.FindAll(t => t.Tour == this._currentTour);
        foreach (var tour in tourList)
        {
            AirConsole.instance.Message(tour.DeviceID, new
            {
                type = "tour", payload = new
                {
                    up = tour.Target.up == ButtonState.Released ? 0 : 1,
                    down = tour.Target.down == ButtonState.Released ? 0 : 1,
                    left = tour.Target.left == ButtonState.Released ? 0 : 1,
                    right = tour.Target.right == ButtonState.Released ? 0 : 1
                }
            });
        }
    }

    private void nextTour()
    {
        this.endTour();
        this._currentTour++;
        if (this._currentTour > this.maxSteps)
        {
            this.gameStarted = false;
            this.endGame();
            return;
        }

        this.startTour();
    }

    private void endGame()
    {
        foreach (var playerId in _playerDeviceIdList)
        {
            AirConsole.instance.Message(playerId, new
            {
                type = "state",
                sreen = "GameOver",
                score = totalPercentage * 100
            });
        }

        Debug.Log("Game Ended, Score: %" + (this.totalPercentage * 100));
    }

    private void endTour()
    {
        this.secondsPassed = 0;
        this.lastSecond = (int) Time.timeSinceLevelLoad % 60;

        foreach (var player in _playerDeviceIdList)
        {
            var tourIndex = _playerTours.FindIndex(t => t.DeviceID == player && t.Tour == _currentTour);
            _playerTours[tourIndex] = new PlayerTour(
                _playerTours[tourIndex].Tour,
                player,
                _playerTours[tourIndex].Target,
                _playerTours[tourIndex].Target.CompareTo((getPadStateOfPlayer(player))));
        }

        this.calculateTotalPercentage();
    }

    private void calculateTotalPercentage()
    {
        var completed_tours = this._playerTours.FindAll(t => this._currentTour >= t.Tour);
        var successful_tours = completed_tours.FindAll(t => t.Successful);
        this.totalPercentage = (float) successful_tours.Count / this._playerTours.Count;
        this._parent.interpolationTime = this.totalPercentage;
    }

    private void OnDisconnect(int deviceId)
    {
        if (this._playerDeviceIdList.Exists(t => t == deviceId))
        {
            this._playerDeviceIdList.Remove(deviceId);
        }
    }

    private void OnConnect(int deviceId)
    {
        if (gameStarted) return;
        if (!this._playerDeviceIdList.Exists(t => t == deviceId))
        {
            this._playerDeviceIdList.Add(deviceId);
        }
    }

    private static void OnMessage(int @from, JToken data)
    {
        string type = (string) data["type"];
        if (type == "button")
        {
            object payload = (object) data["payload"];
            if (payload != null)
            {
                int up = Convert.ToInt32(data["payload"]["up"].ToString());
                int down = Convert.ToInt32(data["payload"]["down"].ToString());
                int left = Convert.ToInt32(data["payload"]["left"].ToString());
                int right = Convert.ToInt32(data["payload"]["right"].ToString());

                PadState state = new PadState(
                    left == 0 ? ButtonState.Released : ButtonState.Pressed,
                    right == 0 ? ButtonState.Released : ButtonState.Pressed,
                    up == 0 ? ButtonState.Released : ButtonState.Pressed,
                    down == 0 ? ButtonState.Released : ButtonState.Pressed
                );

                if (!PlayerPadState.ContainsKey(from))
                {
                    PlayerPadState.Add(from, state);
                }
                else
                {
                    PlayerPadState[from] = state;
                }
            }
        }

        if (type == "state")
        {
            var isReady = (bool) data["ready"];
            if (!isReady)
            {
                if (_readyPlayerList.Contains(from)) _readyPlayerList.Remove(from);
            }
            else
            {
                if (!_readyPlayerList.Contains(from)) _readyPlayerList.Add(from);
            }

            Debug.Log("State From: " + from + " Type: State | Ready: " + isReady + " Ready Player Count: " +
                      _readyPlayerList.Count);
        }
    }

    private class PlayerTour
    {
        public int Tour;
        public int DeviceID;
        public PadState Target;
        public bool Successful;

        public PlayerTour(int tour, int deviceId, PadState target, bool successful)
        {
            Tour = tour;
            DeviceID = deviceId;
            Target = target;
            Successful = successful;
        }
    }

    private struct PadState
    {
        public ButtonState left;
        public ButtonState right;
        public ButtonState up;
        public ButtonState down;

        public PadState(ButtonState left, ButtonState right, ButtonState up, ButtonState down)
        {
            this.left = left;
            this.right = right;
            this.up = up;
            this.down = down;
        }

        public override string ToString()
        {
            string result = "";
            if (this.left == ButtonState.Pressed)
            {
                result += "A ";
            }

            if (this.right == ButtonState.Pressed)
            {
                result += "D ";
            }

            if (this.up == ButtonState.Pressed)
            {
                result += "W ";
            }

            if (this.down == ButtonState.Pressed)
            {
                result += "S ";
            }

            return result;
        }

        public bool CompareTo(PadState other)
        {
            return
                this.left == other.left &&
                this.right == other.right &&
                this.up == other.up &&
                this.down == other.down;
        }
    }

    private enum ButtonState
    {
        Released = 0,
        Pressed = 1,
    }
}