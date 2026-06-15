using System;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace StructureFailureSimulator
{
    public partial class MainWindow : Window
    {
        private GameManager _game = new();
        private DispatcherTimer _timer = new();

        private enum ToolMode
        {
            Node,
            SupportNode,
            Connect
        }

        private ToolMode _tool = ToolMode.Node;
        private Node _selectedNode;

        public MainWindow()
        {
            InitializeComponent();

            _game.StartNewRun(seed: 12345);

            _timer.Interval = TimeSpan.FromMilliseconds(50);
            _timer.Tick += Tick;
            _timer.Start();

            Draw();
        }

        // =========================
        // GAME LOOP
        // =========================
        private void Tick(object sender, EventArgs e)
        {
            if (_game.State == GameState.Simulate)
            {
                _game.Engine.Step();

                // auto-end if everything fails (simple win/lose condition)
                if (_game.Structure.Members.Count > 0 &&
                    _game.Structure.Members.All(m => m.Failed))
                {
                    _game.EndSimulation();
                }
            }

            Draw();
        }

        // =========================
        // INPUT (BUILD MODE)
        // =========================
        private void Canvas_Click(object sender, MouseButtonEventArgs e)
        {
            if (_game.State != GameState.Build)
                return;

            var pos = e.GetPosition(RenderCanvas);
            var v = new Vector2((float)pos.X, (float)pos.Y);

            if (_tool == ToolMode.Node)
            {
                _game.Structure.Nodes.Add(new Node
                {
                    Position = v
                });
            }
            else if (_tool == ToolMode.SupportNode)
            {
                _game.Structure.Nodes.Add(new Node
                {
                    Position = v,
                    IsFixedSupport = true
                });
            }
            else if (_tool == ToolMode.Connect)
            {
                var clicked = FindClosestNode(v.X, v.Y);

                if (clicked == null) return;

                if (_selectedNode == null)
                {
                    _selectedNode = clicked;
                }
                else
                {
                    if (_selectedNode != clicked)
                    {
                        _game.Structure.Members.Add(new Member
                        {
                            A = _selectedNode,
                            B = clicked
                        });
                    }

                    _selectedNode = null;
                }
            }

            Draw();
        }

        private Node FindClosestNode(double x, double y)
        {
            Node best = null;
            double bestDist = double.MaxValue;

            foreach (var n in _game.Structure.Nodes)
            {
                double dx = n.Position.X - x;
                double dy = n.Position.Y - y;
                double d = dx * dx + dy * dy;

                if (d < 400 && d < bestDist)
                {
                    bestDist = d;
                    best = n;
                }
            }

            return best;
        }

        // =========================
        // UI BUTTONS
        // =========================
        private void ToolNode_Click(object sender, RoutedEventArgs e)
        {
            _tool = ToolMode.Node;
        }

        private void ToolSupport_Click(object sender, RoutedEventArgs e)
        {
            _tool = ToolMode.SupportNode;
        }

        private void ToolConnect_Click(object sender, RoutedEventArgs e)
        {
            _tool = ToolMode.Connect;
        }

        private void StartRun_Click(object sender, RoutedEventArgs e)
        {
            if (_game.State == GameState.Build)
            {
                _game.BeginSimulation();
            }
        }

        private void StopRun_Click(object sender, RoutedEventArgs e)
        {
            _game.EndSimulation();
        }

        private void ReturnToBuild_Click(object sender, RoutedEventArgs e)
        {
            _game.ReturnToBuild();
            _selectedNode = null;
        }

        // =========================
        // RENDERING
        // =========================
        private void Draw()
        {
            RenderCanvas.Children.Clear();

            var structure = _game.Structure;

            Title =
                $"State: {_game.State} | " +
                $"Day: {_game.Run?.Day ?? 0} | " +
                $"Wind: {_game.Run?.WindStrength ?? 0:0.0} | " +
                $"Seismic: {_game.Run?.SeismicPulse ?? 0:0.0}";

            // draw members
            foreach (var m in structure.Members)
            {
                var line = new Line
                {
                    X1 = m.A.Position.X,
                    Y1 = m.A.Position.Y,
                    X2 = m.B.Position.X,
                    Y2 = m.B.Position.Y,
                    StrokeThickness = 3
                };

                if (m.Failed)
                {
                    line.Stroke = Brushes.Red;
                }
                else
                {
                    double ratio = m.CurrentLoad / Math.Max(1, m.Capacity);

                    line.Stroke = ratio switch
                    {
                        < 0.5 => Brushes.LimeGreen,
                        < 0.8 => Brushes.Yellow,
                        < 1.0 => Brushes.Orange,
                        _ => Brushes.Magenta
                    };
                }

                RenderCanvas.Children.Add(line);
            }

            // draw nodes
            foreach (var n in structure.Nodes)
            {
                var ell = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = n.IsFixedSupport ? Brushes.Cyan : Brushes.White,
                    Stroke = (n == _selectedNode) ? Brushes.Red : null,
                    StrokeThickness = 2
                };

                Canvas.SetLeft(ell, n.Position.X - 5);
                Canvas.SetTop(ell, n.Position.Y - 5);

                RenderCanvas.Children.Add(ell);
            }

            // build-mode hint overlay (simple)
            if (_game.State == GameState.Build)
            {
                var text = new System.Windows.Controls.TextBlock
                {
                    Text = $"BUILD MODE: {_tool}",
                    Foreground = Brushes.White,
                    FontSize = 16
                };

                Canvas.SetLeft(text, 10);
                Canvas.SetTop(text, 10);

                RenderCanvas.Children.Add(text);
            }

            // results overlay
            if (_game.State == GameState.Results)
            {
                var text = new System.Windows.Controls.TextBlock
                {
                    Text = "STRUCTURE FAILED - PRESS RETURN TO REBUILD",
                    Foreground = Brushes.Red,
                    FontSize = 20
                };

                Canvas.SetLeft(text, 200);
                Canvas.SetTop(text, 200);

                RenderCanvas.Children.Add(text);
            }
        }
    }
}