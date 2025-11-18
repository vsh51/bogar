using System.Diagnostics;
using System.Collections.Generic;
using System;
using Bogar.BLL.Core;

namespace Bogar.BLL.Player;

public class Player : IPlayer
{
    private readonly string _path;

    public Player(string path)
    {
        _path = path;
    }
    
    public string GetMove(List<Move> moves) 
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = _path,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        using (Process process = new Process())
        {
            process.StartInfo = startInfo;
            try
            {
                process.Start();
                using (StreamWriter writer = process.StandardInput)
                {
                    writer.WriteLine(moves.Count);

                    foreach (Move move in moves)
                    {
                        writer.WriteLine(move.ToString());
                    }
                } 

                string newMove = process.StandardOutput.ReadLine();
                string errors = process.StandardError.ReadToEnd();

                process.WaitForExit();
                if (!string.IsNullOrEmpty(errors))
                {
                    throw new Exception(
                        $"Помилка виконання бота ({_path}): {errors}"
                    );
                }

                return newMove ?? string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Не вдалося запустити бота: {ex.Message}");
                return string.Empty; 
            }
        }
    }
}
