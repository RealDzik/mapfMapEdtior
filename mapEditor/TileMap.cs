using System;
using System.Collections.Generic;
using System.IO;


namespace mapEditor
{
    public class TileMap
{
    private int _width;
    private int _height;
    private char[,] _mapData;

    public int Height => _height;
    public int Width  => _width;

    // 简化处理，只切换 '.' 和 '@'
    private readonly char _emptyChar   = '.';
    private readonly char _blockedChar = '@';

    public TileMap(string mapFilePath)
    {
        // 从文件加载地图行
        var lines = File.ReadAllLines(mapFilePath);

        // 假设文件格式:
        // type octile
        // height 256
        // width 256
        // map
        // .............
        // @@@@@@@@@....
        // ...
        // 本示例仅做简单解析，请根据实际文件格式改进
        int widthLineIndex = Array.FindIndex(lines, x => x.StartsWith("width"));
        int heightLineIndex = Array.FindIndex(lines, x => x.StartsWith("height"));
        int mapLineIndex = Array.FindIndex(lines, x => x.StartsWith("map"));

        if (widthLineIndex < 0 || heightLineIndex < 0 || mapLineIndex < 0)
            throw new Exception("地图文件格式或关键字不正确，请检查。");

        // 从文本读取宽/高
        _width  = int.Parse(lines[widthLineIndex].Split(' ')[1]);
        _height = int.Parse(lines[heightLineIndex].Split(' ')[1]);

        // 准备存储地图字符的 2D 数组
        _mapData = new char[_height, _width];

        // map 行下面开始才是真正的地图数据，逐行读取
        for (int row = 0; row < _height; row++)
        {
            var lineContent = lines[mapLineIndex + 1 + row];
            for (int col = 0; col < _width; col++)
            {
                _mapData[row, col] = lineContent[col];
            }
        }
    }

    // 新增：从给定的宽高构造一个空地图（默认用 '.' 填充）
    public TileMap(int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentException("地图宽高必须为正整数。");

        _width = width;
        _height = height;
        _mapData = new char[_height, _width];

        // 全部填充为 '.' 表示空地
        for (int r = 0; r < _height; r++)
        {
            for (int c = 0; c < _width; c++)
            {
                _mapData[r, c] = _emptyChar; // 即 '.'
            }
        }
    }

    /// <summary>
    /// 返回第 row 行、第 col 列的瓦片字符
    /// </summary>
    public char GetTile(int row, int col)
    {
        return _mapData[row, col];
    }

    /// <summary>
    /// 切换指定点的瓦片字符：. 与 @。
    /// </summary>
    public void ToggleTileState(int row, int col)
    {
        if (row < 0 || row >= _height ||
            col < 0 || col >= _width)
        {
            return;
        }

        if (_mapData[row, col] == _emptyChar)
        {
            _mapData[row, col] = _blockedChar;
        }
        else if (_mapData[row, col] == _blockedChar)
        {
            _mapData[row, col] = _emptyChar;
        }
    }

    /// <summary>
    /// 保存当前地图到文件，覆盖写回
    /// </summary>
    public void SaveMap(string mapFilePath)
    {
        var outputLines = new List<string>
        {
            "type octile",
            $"height {_height}",
            $"width {_width}",
            "map"
        };

        for (int r = 0; r < _height; r++)
        {
            char[] lineChars = new char[_width];
            for (int c = 0; c < _width; c++)
            {
                lineChars[c] = _mapData[r, c];
            }
            outputLines.Add(new string(lineChars));
        }

        File.WriteAllLines(mapFilePath, outputLines);
    }

    public void SetTileState(int row, int col, char newChar)
    {
        if (row < 0 || row >= _height || col < 0 || col >= _width)
        {
            return;
        }
        _mapData[row, col] = newChar;
    }
} 
}
