using System;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Threading;

namespace StructureFailureSimulator
{
    public partial class MainWindow : Window
    {
        // =========================
        // CORE SIMULATION
        // =========================
        private readonly SimulationEngine _engine = new SimulationEngine();
        private readonly Structure _structure = new Structure();
        private readonly RunContext _run = new RunContext(Environment.TickCount);

        private readonly DispatcherTimer _timer = new DispatcherTimer();

        // =========================
        // GAME STATE
        // =========================
        private enum GameState
        {
            Build,
            Simulate,
            Results
        }

        private GameState _state = GameState.Build;

        private enum ToolMode
        {
            Node,
            Support,
            Connect
        }

        private ToolMode _tool = ToolMode.Node;
        private Node _selectedNode;

        // =========================
        // CAMERA
        // =========================
        private Vector2 _cameraOffset = new Vector2(0, 0);
        private double _zoom = 1.0;

        private const double DisplacementScale = 60.0;

        public MainWindow()
        {
            InitializeComponent();

            _engine.Initialize(_structure, _run);

            _timer.Interval = TimeSpan.FromMilliseconds(33);
            _timer.Tick += Tick;
            _timer.Start();
        }

        // =========================
        // GAME LOOP
        // =========================
        private void Tick(object sender, EventArgs e)
        {
            if (_state == GameState.Simulate)
            {
                _engine.Step();
            }

            Draw();
        }

        // =========================
        // CAMERA TRANSFORM
        // =========================
        private Vector2 WorldToScreen(Vector2 p)
        {
            return new Vector2(
                (p.X + _cameraOffset.X) * (float)_zoom,
                (p.Y + _cameraOffset.Y) * (float)_zoom
            );
        }

        private void CenterCamera()
        {
            if (_structure.Nodes.Count == 0) return;

            float avgX = _structure.Nodes.Average(n => n.Position.X);
            float avgY = _structure.Nodes.Average(n => n.Position.Y);

            _cameraOffset = new Vector2(
                (float)(RenderCanvas.ActualWidth / 2 / _zoom - avgX),
                (float)(RenderCanvas.ActualHeight / 2 / _zoom - avgY)
            );
        }

        // =========================
        // INPUT (BUILD MODE)
        // =========================
        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_state != GameState.Build)
                return;

            var pos = e.GetPosition(RenderCanvas);

            Vector2 world = new Vector2(
                (float)(pos.X / _zoom - _cameraOffset.X),
                (float)(pos.Y / _zoom - _cameraOffset.Y)
            );

            if (_tool == ToolMode.Node)
            {
                _structure.Nodes.Add(new Node
                {
                    Position = world
                });
            }
            else if (_tool == ToolMode.Support)
            {
                _structure.Nodes.Add(new Node
                {
                    Position = world,
                    IsFixedSupport = true
                });
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
                    }

                    _selectedNode = null;
                }
            }
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
        private void ToolNode_Click(object sender, RoutedEventArgs e)
            => _tool = ToolMode.Node;

        private void ToolSupport_Click(object sender, RoutedEventArgs e)
            => _tool = ToolMode.Support;

        private void ToolConnect_Click(object sender, RoutedEventArgs e)
            => _tool = ToolMode.Connect;

        // =========================
        // SIMULATION CONTROLS
        // =========================
        private void StartRun_Click(object sender, RoutedEventArgs e)
        {
            _state = GameState.Simulate;
        }

        private void StopRun_Click(object sender, RoutedEventArgs e)
        {
            _state = GameState.Results;
        }

        private void ReturnToBuild_Click(object sender, RoutedEventArgs e)
        {
            _state = GameState.Build;
            _selectedNode = null;
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            _structure.Nodes.Clear();
            _structure.Members.Clear();
            _selectedNode = null;
        }

        // =========================
        // RENDERING
        // =========================
        private void Draw()
        {
            RenderCanvas.Children.Clear();

            CenterCamera();

            // =========================
            // MEMBERS (FEM DEFORMED)
            // =========================
            foreach (var m in _structure.Members)
            {
                Vector2 aWorld = new Vector2(
                    m.A.Position.X + (float)(m.A.Ux * DisplacementScale),
                    m.A.Position.Y + (float)(m.A.Uy * DisplacementScale)
                );

                Vector2 bWorld = new Vector2(
                    m.B.Position.X + (float)(m.B.Ux * DisplacementScale),
                    m.B.Position.Y + (float)(m.B.Uy * DisplacementScale)
                );

                Vector2 a = WorldToScreen(aWorld);
                Vector2 b = WorldToScreen(bWorld);

                double stress = Math.Abs(m.AxialForce) / Math.Max(1, m.YieldStrength);

                Brush color =
                    m.Failed ? Brushes.Red :
                    stress < 0.5 ? Brushes.LimeGreen :
                    stress < 0.8 ? Brushes.Yellow :
                    stress < 1.0 ? Brushes.Orange :
                    Brushes.Magenta;

                var line = new Line
                {
                    X1 = a.X,
                    Y1 = a.Y,
                    X2 = b.X,
                    Y2 = b.Y,
                    Stroke = color,
                    StrokeThickness = m.Failed ? 4 : 2,
                    Opacity = m.Failed ? 0.4 : 1.0
                };

                RenderCanvas.Children.Add(line);
            }

            // =========================
            // NODES
            // =========================
            foreach (var n in _structure.Nodes)
            {
                Vector2 world = new Vector2(
                    n.Position.X + (float)(n.Ux * DisplacementScale),
                    n.Position.Y + (float)(n.Uy * DisplacementScale)
                );

                Vector2 screen = WorldToScreen(world);

                var ell = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = n.IsFixedSupport ? Brushes.Cyan : Brushes.White,
                    Stroke = (n == _selectedNode) ? Brushes.Red : null,
                    StrokeThickness = 2
                };

                Canvas.SetLeft(ell, screen.X - 5);
                Canvas.SetTop(ell, screen.Y - 5);

                RenderCanvas.Children.Add(ell);
            }

            // =========================
            // UI OVERLAY
            // =========================
            var text = new TextBlock
            {
                Text =
                    $"State: {_state}  |  Nodes: {_structure.Nodes.Count}  |  Members: {_structure.Members.Count}",
                Foreground = Brushes.White,
                FontSize = 14
            };

            Canvas.SetLeft(text, 10);
            Canvas.SetTop(text, 10);

            RenderCanvas.Children.Add(text);
        }
    }
}