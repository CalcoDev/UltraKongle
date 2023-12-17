using System.Linq;
using Godot;
using Godot.Collections;
using KongleJam.GameObjects;
using KongleJam.Managers;
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

	private TextureRect _selectedRect;
	private int _selectedId;

	private TextureButton _readyBtn;

	private Timer _countdownTimer;
	private Label _countdownLabel;
	private Label _readyCountLabel;

	private Control _characterDisplays;

	private int _readiedPlayers = 0;

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
				if (_selectedRect != select)
					select.Texture = _hoveredTexture;
				
				_currentRect = select;
				_currentId = int.Parse(select.Name.ToString()[4..]) - 1;
			};
			select.MouseExited += () => {
				if (_selectedRect != select)
					select.Texture = _defaultTexture;
				
				_currentRect = null;
				_currentId = -1;
			};
			
			idx += 1;
		}

		_readyBtn = GetNode<TextureButton>("%PlayButton");
		_readyBtn.Connect(
			TextureButton.SignalName.ButtonDown,
			new Callable(this, MethodName.HandleReady)	
		);

		_characterName = GetNode<Label>("%CharacterName");
		_characterDescription = GetNode<Label>("%CharacterDescription");

		_countdownTimer = GetNode<Timer>("CountdownTimer");
		NetworkManager.Instance.OnSetCountdown += (seconds) => {
			_countdownLabel.Visible = true;
			_readyCountLabel.Visible = true;
			_readiedPlayers += 1;
			_readyCountLabel.Text = $"{_readiedPlayers}/{NetworkManager.Instance.Players.Count}";
			if (_countdownTimer.TimeLeft == 0 || _countdownTimer.TimeLeft > seconds)
				_countdownTimer.Start(seconds);
		};

		_countdownLabel = GetNode<Label>("%CountdownLabel");
		_countdownLabel.Visible = false;
		_readyCountLabel = GetNode<Label>("%ReadyCountLabel");
		_readyCountLabel.Visible = false;
		
		_characterDisplays = GetNode<Control>("%CharacterDisplays");
		NetworkManager.Instance.OnPlayerSelectCharacter += HandlePlayerSelectCharacter;

		// Initial update of character
		_selectedId = 0;
		_selectedRect = _characterSelects.GetChild<TextureRect>(0);
		_selectedRect.Texture = _selectedTexture;
		NetworkManager.Instance.UpdatePlayerCharacter(_selectedId);
	}

    public override void _ExitTree()
    {
		NetworkManager.Instance.OnPlayerSelectCharacter -= HandlePlayerSelectCharacter;
	}

    // NETWORK MANAGER HANDLERS
    private void HandlePlayerSelectCharacter(long id)
	{
		int idx = NetworkManager.Instance.Players[id].Index;
		int charIdx = NetworkManager.Instance.Players[id].CharacterId;
		Character character = _characters[charIdx];
		TextureRect tr = _characterDisplays
			.GetChild(idx)
			.GetNode<TextureRect>("Texture");
		tr.Texture = character.Texture;
		tr.SetAnchorsPreset(Control.LayoutPreset.Center);
	}

    public override void _Process(double delta)
    {
		if (Input.IsMouseButtonPressed(MouseButton.Left)
			&& _currentRect != null && _currentRect != _selectedRect)
		{
			if (_selectedRect != null)
				_selectedRect.Texture = _defaultTexture;

			_currentRect.Texture = _selectedTexture;
			_selectedId = _currentId;
			_selectedRect = _currentRect;
			NetworkManager.Instance.UpdatePlayerCharacter(_selectedId);
			UI_SelectCharacter();
		}

		_countdownLabel.Text = $"{(int)_countdownTimer.TimeLeft + 1}";
		Vector2 mouseOffset =
			GetLocalMousePosition() / GetViewportRect().Size * 2f - Vector2.One;
		
		_backgroundMat.SetShaderParameter("offset", mouseOffset);
    }

	private void HandleReady()
	{
		_readyBtn.Disabled = true;
		NetworkManager.Instance.AnnounceReady();
	}
				
	private void UI_SelectCharacter()
	{
		_characterName.Text = _characters[_selectedId].Name;
		_characterDescription.Text = _characters[_selectedId].Description;

		// TODO(calo): Shop preview etc etc
	}
}
