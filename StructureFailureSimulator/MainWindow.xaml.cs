using System;
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
        private readonly DispatcherTimer _simTimer = new DispatcherTimer();

        private readonly SimulationEngine _engine = new SimulationEngine();
        private readonly Structure _structure = new Structure();
        private readonly RunContext _run = new RunContext(Environment.TickCount);

        private enum ToolMode
        {
            Node,
            Support,
            Connect
        }

        private ToolMode _tool = ToolMode.Node;
        private Node _selectedNode;

        private GameState _state = GameState.Build;

        // ONLY redraw when needed
        private bool _needsRedraw = true;

        private const double DisplacementScale = 60.0;

        public MainWindow()
        {
            InitializeComponent();

            _simTimer.Interval = TimeSpan.FromMilliseconds(50); // controllable sim speed
            _simTimer.Tick += SimTick;

            _engine.Initialize(_structure, _run);

            Draw(); // initial render
        }

        private void SimTick(object sender, EventArgs e)
        {
            if (_state != GameState.Simulate)
                return;

            _engine.Step();

            _needsRedraw = true;
            Draw();
        }

        // =========================
        // CAMERA (kept simple / stable)
        // =========================
        private Vector2 WorldToScreen(Vector2 p)
        {
            return new Vector2(p.X, p.Y);
        }

        private Vector2 Deform(Node n)
        {
            return new Vector2(
                n.Position.X + (float)(n.Ux * DisplacementScale),
                n.Position.Y + (float)(n.Uy * DisplacementScale)
            );
        }

        // =========================
        // CLICK INPUT (INSTANT)
        // =========================
        private void Canvas_Click(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(RenderCanvas);

            Vector2 world = new Vector2((float)pos.X, (float)pos.Y);

            if (_state != GameState.Build)
                return;

            if (_tool == ToolMode.Node)
            {
                _structure.Nodes.Add(new Node
                {
                    Position = world
                });

                _needsRedraw = true;
            }
            else if (_tool == ToolMode.Support)
            {
                _structure.Nodes.Add(new Node
                {
                    Position = world,
                    IsFixedSupport = true
                });

                _needsRedraw = true;
            }
            else if (_tool == ToolMode.Connect)
            {
                var n = FindClosestNode(world);
                if (n == null) return;

                if (_selectedNode == null)
                {
                    _selectedNode = n;
                }
                else
                {
                    if (_selectedNode != n)
                    {
                        _structure.Members.Add(new Member
                        {
                            A = _selectedNode,
                            B = n
                        });

                        _needsRedraw = true;
                    }

                    _selectedNode = null;
                }
            }

            Draw(); // immediate feedback (NO TIMER DELAY)
        }

        private Node FindClosestNode(Vector2 p)
        {
            Node best = null;
            float bestDist = float.MaxValue;

            foreach (var n in _structure.Nodes)
            {
                float dx = n.Position.X - p.X;
                float dy = n.Position.Y - p.Y;

                float d = dx * dx + dy * dy;

                if (d < 5000 && d < bestDist)
                {
                    bestDist = d;
                    best = n;
                }
            }

            return best;
        }

        // =========================
        // TOOL BUTTONS
        // =========================
        private void ToolNode_Click(object sender, RoutedEventArgs e) => _tool = ToolMode.Node;
        private void ToolSupport_Click(object sender, RoutedEventArgs e) => _tool = ToolMode.Support;
        private void ToolConnect_Click(object sender, RoutedEventArgs e) => _tool = ToolMode.Connect;

        private void StartRun_Click(object sender, RoutedEventArgs e)
        {
            _state = GameState.Simulate;
            _simTimer.Start();
        }

        private void StopRun_Click(object sender, RoutedEventArgs e)
        {
            _state = GameState.Build;
            _simTimer.Stop();
        }

        private void StepRun_Click(object sender, RoutedEventArgs e)
        {
            _engine.Step();
            _needsRedraw = true;
            Draw();
        }

        private void ReturnToBuild_Click(object sender, RoutedEventArgs e)
        {
            _state = GameState.Build;
            _selectedNode = null;

            Draw();
        }

        // =========================
        // SIMULATION STEP (MANUAL CONTROL OR BUTTON DRIVEN)
        // =========================
        private void StepSimulation()
        {
            _engine.Step();
            _needsRedraw = true;
            Draw();
        }

        // =========================
        // RENDERING (ONLY WHEN NEEDED)
        // =========================
        private void Draw()
        {
            if (!_needsRedraw)
                return;

            RenderCanvas.Children.Clear();

            // MEMBERS
            foreach (var m in _structure.Members)
            {
                Vector2 a = WorldToScreen(Deform(m.A));
                Vector2 b = WorldToScreen(Deform(m.B));

                double stress = Math.Abs(m.AxialForce) / Math.Max(1, m.YieldStrength);

                Brush color =
                    m.Failed ? Brushes.Red :
                    stress < 0.5 ? Brushes.LimeGreen :
                    stress < 0.8 ? Brushes.Yellow :
                    stress < 1.0 ? Brushes.Orange :
                    Brushes.Magenta;

                RenderCanvas.Children.Add(new Line
                {
                    X1 = a.X,
                    Y1 = a.Y,
                    X2 = b.X,
                    Y2 = b.Y,
                    Stroke = color,
                    StrokeThickness = m.Failed ? 4 : 2
                });
            }

            // NODES
            foreach (var n in _structure.Nodes)
            {
                Vector2 p = WorldToScreen(Deform(n));

                var ell = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = n.IsFixedSupport ? Brushes.Cyan : Brushes.White
                };

                RenderCanvas.Children.Add(ell);

                Canvas.SetLeft(ell, p.X - 5);
                Canvas.SetTop(ell, p.Y - 5);
            }

            // UI TEXT
            var text = new TextBlock
            {
                Text = $"State: {_state} | Nodes: {_structure.Nodes.Count} | Members: {_structure.Members.Count}",
                Foreground = Brushes.White
            };

            RenderCanvas.Children.Add(text);
            Canvas.SetLeft(text, 10);
            Canvas.SetTop(text, 10);

            _needsRedraw = false;
        }
    }

    public enum GameState
    {
        Build,
        Simulate
    }
}