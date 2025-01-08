using Microsoft.Win32;  // 用于 OpenFileDialog
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;  // 用于 Path.GetExtension 等

namespace mapEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TileMap _tileMap;
        private const int TileSize = 3;

        // 新增：存储每个地图瓦片对应的 Rectangle
        private Rectangle[,] _tileRects;

        // 新增：标记鼠标左键是否按下
        private bool _isMouseDown;
        // 上一次处理的格子坐标，避免短时间内多次切换同一块瓦片
        private int _lastRow = -1;
        private int _lastCol = -1;

        // 新增：记录这次拖动要把瓦片改成什么字符('.'或'@')
        private char? _dragChar = null;

        // 新增：每次拖动可更改的格子数量，默认值为1
        private int _tilesToChange = 3;

        // 新增：用于控制画布的缩放变换
        private ScaleTransform _canvasScaleTransform = new ScaleTransform(1.0, 1.0);
        private TranslateTransform _canvasTranslateTransform = new TranslateTransform(0, 0);
        private TransformGroup _canvasTransformGroup = new TransformGroup();

        // 新增：用于追踪鼠标中键的平移状态
        private bool _isMiddleMouseDown = false;
        private Point _lastMiddleMousePos;

        public MainWindow()
        {
            InitializeComponent();
            _tileMap = new TileMap("tileMap/Berlin_1_256.map");
            
            // 将缩放和位移变换加入 TransformGroup
            _canvasTransformGroup.Children.Add(_canvasScaleTransform);
            _canvasTransformGroup.Children.Add(_canvasTranslateTransform);

            // 将 TransformGroup 赋给画布
            MapCanvas.RenderTransform = _canvasTransformGroup;

            DrawMap();
        }

        private void DrawMap()
        {
            // 若已存在瓦片矩形数组，可能是重新画的需求，可先清理 Canvas
            MapCanvas.Children.Clear();

            // 初始化数组大小
            _tileRects = new Rectangle[_tileMap.Height, _tileMap.Width];

            for (int r = 0; r < _tileMap.Height; r++)
            {
                for (int c = 0; c < _tileMap.Width; c++)
                {
                    char tileChar = _tileMap.GetTile(r, c);

                    var rect = new Rectangle
                    {
                        Width = TileSize,
                        Height = TileSize,
                        Fill = (tileChar == '.') ? Brushes.White : Brushes.Black
                    };

                    Canvas.SetLeft(rect, c * TileSize);
                    Canvas.SetTop(rect, r * TileSize);
                    MapCanvas.Children.Add(rect);

                    // 将此 Rectangle 存到二维数组中，方便局部更新
                    _tileRects[r, c] = rect;
                }
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isMouseDown = true;
            // 每次新开一次拖动时重置上次行列
            _lastRow = -1;
            _lastCol = -1;

            // 首次决定 _dragChar
            SetDragCharBasedOnFirstTile(e);

            // 设置第一块瓦片
            SetTileUnderMouse(e);
        }

        // 新增：鼠标移动时判定是否在拖动
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isMouseDown)
            {
                SetTileUnderMouse(e);
            }
        }

        // 新增：鼠标左键抬起或鼠标离开窗口时，停止拖动
        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isMouseDown = false;
            _lastRow = -1;
            _lastCol = -1;

            // 本次拖动结束后，清空 _dragChar 以示完成
            _dragChar = null;
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            // 也可视需求决定离开时是否停止拖动以防误操作
            _isMouseDown = false;
            _lastRow = -1;
            _lastCol = -1;
            _dragChar = null;
        }

        /// <summary>
        /// 根据首次点击时瓦片的现状，决定整个拖动过程中要用的新字符
        /// </summary>
        private void SetDragCharBasedOnFirstTile(MouseEventArgs e)
        {
            Point p = e.GetPosition(MapCanvas);
            int row = (int)(p.Y / TileSize);
            int col = (int)(p.X / TileSize);

            // 检查越界
            if (row < 0 || row >= _tileMap.Height ||
                col < 0 || col >= _tileMap.Width)
            {
                _dragChar = null;
                return;
            }

            // 如果当前瓦片是 '.'，则整次拖动把它们都改成 '@'
            // 如果当前瓦片是 '@'，则都改成 '.'
            char current = _tileMap.GetTile(row, col);
            _dragChar = (current == '.') ? '@' : '.';
        }

        /// <summary>
        /// 将鼠标当前位置对应的瓦片设置成这次拖动的目标字符
        /// </summary>
        private void SetTileUnderMouse(MouseEventArgs e)
        {
            if (_dragChar == null) return;

            Point p = e.GetPosition(MapCanvas);
            int row = (int)(p.Y / TileSize);
            int col = (int)(p.X / TileSize);

            // 防止越界
            if (row < 0 || row >= _tileMap.Height ||
                col < 0 || col >= _tileMap.Width)
            {
                return;
            }

            // 避免连续在同一瓦片刷新
            if (row == _lastRow && col == _lastCol)
            {
                return;
            }

            // 更新多个格子的状态
            for (int r = row; r < row + _tilesToChange && r < _tileMap.Height; r++)
            {
                for (int c = col; c < col + _tilesToChange && c < _tileMap.Width; c++)
                {
                    _tileMap.SetTileState(r, c, _dragChar.Value);
                    _tileRects[r, c].Fill = (_dragChar.Value == '.') ? Brushes.White : Brushes.Black;
                }
            }

            _lastRow = row;
            _lastCol = col;
        }

        // 新增：菜单栏 "Open"
        private void MenuItem_Open_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "Map Files (*.map)|*.map|All Files (*.*)|*.*",
                Title = "Open Map File"
            };
            if (openDialog.ShowDialog() == true)
            {
                // 根据用户选择的文件重新加载地图
                _tileMap = new TileMap(openDialog.FileName);
                DrawMap();
            }
        }

        // 新增：菜单栏 "Save"
        private void MenuItem_Save_Click(object sender, RoutedEventArgs e)
        {
            // 如果你希望也给用户选择存储路径，可以改成 SaveFileDialog；若只想覆写当前路径，可保持现有逻辑
            _tileMap.SaveMap("tileMap/Berlin_1_256.map");
        }

        // 新增：菜单栏 "Save As"
        private void MenuItem_SaveAs_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "Map Files (*.map)|*.map|All Files (*.*)|*.*",
                Title = "Save Map As",
                FileName = "new.map"  // 默认文件名
            };

            if (saveDialog.ShowDialog() == true)
            {
                // 使用用户选择的文件路径保存地图
                _tileMap.SaveMap(saveDialog.FileName);
            }
        }

        // 新增：菜单栏 "Set Tiles To Change"
        private void MenuItem_SetTilesToChange_Click(object sender, RoutedEventArgs e)
        {
            // 弹出输入框，获取用户输入
            string input = Microsoft.VisualBasic.Interaction.InputBox(
                "请输入每次拖动要更改的格子数量：", 
                "设置格子数量", 
                _tilesToChange.ToString());

            if (int.TryParse(input, out int newTilesToChange) && newTilesToChange > 0)
            {
                _tilesToChange = newTilesToChange;
            }
            else
            {
                MessageBox.Show("请输入一个有效的正整数。", "无效输入", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            // 判断是否拖进了文件类型
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            // 获取拖放进来的文件(可能多个)，此处只用第一个
            var files = (string[]) e.Data.GetData(DataFormats.FileDrop);
            if (files == null || files.Length == 0)
                return;

            string path = files[0];
            // 如果需要限制只打开 .map 文件，做如下判断:
            if (System.IO.Path.GetExtension(path).ToLower() == ".map")
            {
                _tileMap = new TileMap(path);
                DrawMap();
            }
            else
            {
                MessageBox.Show("请拖放 .map 文件", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // 新增：鼠标滚轮事件，用于按住 Ctrl 时缩放画布
        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // 如果按住 Ctrl 键，则进行缩放
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                double zoomStep = 0.1;  // 缩放步长，可根据需要调整

                if (e.Delta > 0)
                {
                    _canvasScaleTransform.ScaleX += zoomStep;
                    _canvasScaleTransform.ScaleY += zoomStep;
                }
                else
                {
                    _canvasScaleTransform.ScaleX -= zoomStep;
                    _canvasScaleTransform.ScaleY -= zoomStep;
                }

                // 防止缩放过小
                if (_canvasScaleTransform.ScaleX < 0.1) _canvasScaleTransform.ScaleX = 0.1;
                if (_canvasScaleTransform.ScaleY < 0.1) _canvasScaleTransform.ScaleY = 0.1;
            }
        }

        // 新增：在 File 菜单中 "New" 菜单项的点击事件
        private void MenuItem_New_Click(object sender, RoutedEventArgs e)
        {
            // 弹出输入对话框，让用户输入地图宽、高
            string widthInput = Microsoft.VisualBasic.Interaction.InputBox(
                "请输入地图宽度：", 
                "新建地图", 
                "32" // 默认值，可按需修改
            );

            string heightInput = Microsoft.VisualBasic.Interaction.InputBox(
                "请输入地图高度：", 
                "新建地图", 
                "32" // 默认值，可按需修改
            );

            if (int.TryParse(widthInput, out int mapWidth) && mapWidth > 0 &&
                int.TryParse(heightInput, out int mapHeight) && mapHeight > 0)
            {
                // 使用用户输入的尺寸创建新的 TileMap
                _tileMap = new TileMap(mapWidth, mapHeight);

                // 绘制新地图
                DrawMap();
            }
            else
            {
                MessageBox.Show("请输入有效的正整数宽度和高度。", "无效输入", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}