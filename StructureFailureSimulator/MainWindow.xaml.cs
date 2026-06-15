using System;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace StructureFailureSimulator
{
    public partial class MainWindow : Window
    {
        private SimulationEngine _engine = new();
        private Structure _structure = new();
        private DispatcherTimer _timer = new();

        public MainWindow()
        {
            InitializeComponent();

            BuildTestStructure();

            _engine.Initialize(_structure);

            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Tick += Tick;
            _timer.Start();
        }

        private void BuildTestStructure()
        {
            // grid of nodes
            int id = 0;

            Node[,] grid = new Node[4, 4];

            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    grid[x, y] = new Node
                    {
                        Id = id++,
                        Position = new Vector2(x * 100 + 200, y * 100 + 100)
                    };

                    _structure.Nodes.Add(grid[x, y]);
                }
            }

            // connect members
            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    if (x < 3)
                        _structure.Members.Add(new Member
                        {
                            A = grid[x, y],
                            B = grid[x + 1, y]
                        });

                    if (y < 3)
                        _structure.Members.Add(new Member
                        {
                            A = grid[x, y],
                            B = grid[x, y + 1]
                        });
                }
            }
        }

        private void Tick(object sender, EventArgs e)
        {
            _engine.Step();
            Draw();
        }

        private void Draw()
        {
            RenderCanvas.Children.Clear();

            foreach (var m in _structure.Members)
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
                    line.Stroke = Brushes.Red;
                else
                {
                    double ratio = m.CurrentLoad / m.Capacity;

                    line.Stroke = ratio switch
                    {
                        < 0.5 => Brushes.Green,
                        < 0.8 => Brushes.Yellow,
                        < 1.0 => Brushes.Orange,
                        _ => Brushes.Magenta
                    };
                }

                RenderCanvas.Children.Add(line);
            }

            foreach (var n in _structure.Nodes)
            {
                var ell = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = Brushes.White
                };

                Canvas.SetLeft(ell, n.Position.X - 4);
                Canvas.SetTop(ell, n.Position.Y - 4);

                RenderCanvas.Children.Add(ell);
            }
        }
    }
}