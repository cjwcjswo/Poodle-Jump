using UnityEngine;
using UnityEditor;
using System.IO;

public class SkyboxSlicer
{
	[MenuItem("Tools/Slice Skybox")]
	static void Slice()
	{
		Texture2D source =
			Selection.activeObject as Texture2D;

		if (source == null)
		{
			Debug.LogError("Texture 선택해라");
			return;
		}

		int w = source.width / 3;
		int h = source.height / 2;

		string path = AssetDatabase.GetAssetPath(source);
		string dir = Path.GetDirectoryName(path);

		string[] names =
		{
			"front","back","left",
			"right","up","down"
		};

		int index = 0;

		for (int y = 1; y >= 0; y--)
		{
			for (int x = 0; x < 3; x++)
			{
				Texture2D tex = new Texture2D(w, h);
				tex.SetPixels(
					source.GetPixels(x * w, y * h, w, h)
				);
				tex.Apply();

				File.WriteAllBytes(
					$"{dir}/{names[index]}.png",
					tex.EncodeToPNG()
				);

				index++;
			}
		}

		AssetDatabase.Refresh();
		Debug.Log("Skybox Slice 완료");
	}
}