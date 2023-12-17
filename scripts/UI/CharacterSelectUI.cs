using System.Linq;
using Godot;
using Godot.Collections;
using KongleJam.GameObjects;
using KongleJam.Resources;

namespace KongleJam.Ui;

public partial class CharacterSelectUI : Node2D
{
	[ExportGroup("Textures")]
	[Export] private Texture2D _defaultTexture;
	[Export] private Texture2D _hoveredTexture;
	[Export] private Texture2D _selectedTexture;

	[ExportGroup("Characters")]
	[Export] private Array<Character> _characters;

	private Camera _cam;
	private ShaderMaterial _backgroundMat;

	private Control _characterSelects;
	private Label _characterName;
	private Label _characterDescription;

	private TextureRect _currentRect;
	private int _currentId;

	private int _selectedId;

	private TextureButton _playBtn;

    public override void _Ready()
    {
		_cam = GetNode<Camera>("%Camera");
		_backgroundMat = GetNode<TextureRect>("%Background").Material as ShaderMaterial;

		// TODO(calco): Add proper camera movement and shake.
		// _cam.ShakeFade(100, 1000, 0.1f);

		_characterSelects = GetNode<Control>("%CharacterSelects");
		int idx = 0;
		foreach (TextureRect select in _characterSelects.GetChildren().Cast<TextureRect>())
		{
			if (idx >= _characters.Count)
			{
				select.Visible = false;
				continue;
			}
			
			TextureRect texture = select.GetNode<TextureRect>("%Texture");
			texture.Texture = _characters[idx].Texture;
			select.MouseEntered += () => {
				texture.Texture = _hoveredTexture;
				_currentRect = select;
				_currentId = int.Parse(select.Name.ToString()[4..]);

				GD.Print($"Entered {_currentId}");
			};
			select.MouseExited += () => {
				GD.Print($"Exit {_currentId}");
				
				texture.Texture = _defaultTexture;
				_currentRect = null;
				_currentId = -1;
			};
			
			idx += 1;
		}

		_playBtn = GetNode<TextureButton>("%PlayButton");
		_playBtn.Connect(
			TextureButton.SignalName.ButtonDown,
			new Callable(this, MethodName.HandlePlay)	
		);
    }

    public override void _Process(double delta)
    {
		if (Input.IsMouseButtonPressed(MouseButton.Left) && _currentRect != null)
		{
			_currentRect.GetNode<TextureRect>("%Texture").Texture = _selectedTexture;
			_selectedId = _currentId;

			// GD.Print($"Selected: {_selectedId}");
			
			// UI_SelectCharacter();
		}

		Vector2 mouseOffset =
			GetLocalMousePosition() / GetViewportRect().Size * 2f - Vector2.One;
		
		_backgroundMat.SetShaderParameter("offset", mouseOffset);
    }

	private void HandlePlay()
	{
		GD.Print($"STARTING PLAY GAME with {_selectedId}!");
	}
				
	private void UI_SelectCharacter()
	{
		_characterName.Text = _characters[_selectedId].Name;
		_characterDescription.Text = _characters[_selectedId].Description;

		// TODO(calo): Shop preview etc etc
	}
}
