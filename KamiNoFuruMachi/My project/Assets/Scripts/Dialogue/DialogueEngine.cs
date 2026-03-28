// Assets/Scripts/Dialogue/DialogueEngine.cs
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KamiNoFuruMachi
{
    public interface IBgPresenter     { UniTask ShowAsync(string bgId, string transition, float duration, CancellationToken ct); }
    public interface ISpritePresenter { UniTask ShowAsync(string charId, string pose, string position, CancellationToken ct); UniTask HideAsync(string charId, CancellationToken ct); }
    public interface ITextPresenter   { UniTask ShowTextAsync(string charName, string body, bool isRead, CancellationToken ct); UniTask WaitForAdvanceAsync(CancellationToken ct); void HideWindow(); }
    public interface IChoicePresenter { UniTask<int> PresentAsync(List<ChoiceOption> options, CancellationToken ct); }
    public interface IBgmPresenter    { UniTask PlayAsync(string bgmId, float fadeDuration, CancellationToken ct); UniTask StopAsync(float fadeDuration, CancellationToken ct); }
    public interface ISePresenter     { void Play(string seId); }
    public interface IEffectPresenter { UniTask ShakeAsync(float intensity, float duration, CancellationToken ct); UniTask FlashAsync(Color color, float duration, CancellationToken ct); UniTask GlitchAsync(float intensity, float duration, CancellationToken ct); UniTask FadeAsync(float duration, bool fadeIn, CancellationToken ct); }
    public interface IFlagManager     { void SetFlag(string key, int value); int GetFlag(string key); bool GetBoolFlag(string key); void SetBoolFlag(string key, bool value); }

    public class DialogueEngine : MonoBehaviour
    {
        [SerializeField] private ScenarioLoader _scenarioLoader;
        [SerializeField] private bool  _enableSkip      = true;
        [SerializeField] private float _skipIntervalSec = 0.05f;

        public event Action<string, string>      OnTextDisplay;
        public event Action<List<ChoiceOption>>  OnChoicePresented;
        public event Action<string>              OnChapterComplete;

        private IBgPresenter     _bgPresenter;
        private ISpritePresenter _spritePresenter;
        private ITextPresenter   _textPresenter;
        private IChoicePresenter _choicePresenter;
        private IBgmPresenter    _bgmPresenter;
        private ISePresenter     _sePresenter;
        private IEffectPresenter _effectPresenter;
        private IFlagManager     _flagManager;

        private int                     _commandIndex;
        private CancellationTokenSource _cts;
        private readonly HashSet<string> _readFlags = new();

        public void Initialize(IBgPresenter bg, ISpritePresenter sprite, ITextPresenter text,
            IChoicePresenter choice, IBgmPresenter bgm, ISePresenter se,
            IEffectPresenter effect, IFlagManager flags)
        {
            _bgPresenter = bg; _spritePresenter = sprite; _textPresenter = text;
            _choicePresenter = choice; _bgmPresenter = bgm; _sePresenter = se;
            _effectPresenter = effect; _flagManager = flags;
        }

        public async UniTask PlayChapterAsync(string chapterId)
        {
            StopEngine();
            _cts = new CancellationTokenSource();
            var scenario = await _scenarioLoader.LoadAsync(chapterId);
            if (scenario == null) { Debug.LogError($"[DialogueEngine] Failed to load: {chapterId}"); return; }
            await RunScenarioAsync(scenario, _cts.Token);
        }

        public void StopEngine() { _cts?.Cancel(); _cts?.Dispose(); _cts = null; }

        private async UniTask RunScenarioAsync(ScenarioData scenario, CancellationToken ct)
        {
            _commandIndex = 0;
            while (_commandIndex < scenario.CommandCount && !ct.IsCancellationRequested)
            {
                var cmd = scenario.GetCommand(_commandIndex);
                if (cmd != null && DialogueCommand.Validate(cmd, out _))
                    await ExecuteCommandAsync(cmd, scenario.chapterId, ct);
                _commandIndex++;
            }
            if (!ct.IsCancellationRequested) OnChapterComplete?.Invoke(scenario.chapterId);
        }

        private async UniTask ExecuteCommandAsync(ScenarioCommand command, string chapterId, CancellationToken ct)
        {
            switch (command.cmd)
            {
                case DialogueCommand.Text:
                    var readKey = $"{chapterId}_{_commandIndex}";
                    var isRead  = _readFlags.Contains(readKey);
                    OnTextDisplay?.Invoke(command.@char ?? "", command.body);
                    if (_textPresenter != null)
                        await _textPresenter.ShowTextAsync(command.@char ?? "", command.body, isRead, ct);
                    if (_enableSkip && isRead)
                        await UniTask.Delay(TimeSpan.FromSeconds(_skipIntervalSec), cancellationToken: ct);
                    else if (_textPresenter != null)
                        await _textPresenter.WaitForAdvanceAsync(ct);
                    _readFlags.Add(readKey);
                    break;

                case DialogueCommand.Choice:
                    OnChoicePresented?.Invoke(command.options);
                    if (_choicePresenter != null)
                    {
                        var idx = await _choicePresenter.PresentAsync(command.options, ct);
                        if (idx >= 0 && idx < command.options.Count && _flagManager != null)
                        {
                            var opt = command.options[idx];
                            if (!string.IsNullOrEmpty(opt.flag))
                            {
                                if (bool.TryParse(opt.value, out var b)) _flagManager.SetBoolFlag(opt.flag, b);
                                else if (int.TryParse(opt.value, out var i)) _flagManager.SetFlag(opt.flag, i);
                            }
                        }
                    }
                    break;

                case DialogueCommand.Bg:
                    if (_bgPresenter != null)
                        await _bgPresenter.ShowAsync(command.id, command.transition ?? DialogueCommand.Transition.Cut, command.duration > 0 ? command.duration : 0.5f, ct);
                    break;

                case DialogueCommand.Sprite:
                    if (_spritePresenter != null)
                    {
                        if (command.pose == "hide")
                            await _spritePresenter.HideAsync(command.@char, ct);
                        else
                            await _spritePresenter.ShowAsync(command.@char, command.pose ?? "normal", command.position ?? DialogueCommand.Position.Center, ct);
                    }
                    break;

                case DialogueCommand.Bgm:
                    if (_bgmPresenter != null)
                        await _bgmPresenter.PlayAsync(command.id, command.fade > 0 ? command.fade : command.duration, ct);
                    break;

                case DialogueCommand.Se:
                    _sePresenter?.Play(command.id);
                    break;

                case DialogueCommand.Effect:
                    if (_effectPresenter != null)
                    {
                        switch (command.type)
                        {
                            case DialogueCommand.EffectType.Shake:  await _effectPresenter.ShakeAsync(command.intensity, command.duration, ct); break;
                            case DialogueCommand.EffectType.Flash:  await _effectPresenter.FlashAsync(ParseColor(command.color, Color.white), command.duration, ct); break;
                            case DialogueCommand.EffectType.Glitch: await _effectPresenter.GlitchAsync(command.intensity, command.duration, ct); break;
                            case DialogueCommand.EffectType.Fade:   await _effectPresenter.FadeAsync(command.duration, command.intensity <= 0, ct); break;
                        }
                    }
                    break;

                case DialogueCommand.Flag:
                    if (_flagManager != null)
                    {
                        if (bool.TryParse(command.value, out var b)) _flagManager.SetBoolFlag(command.key, b);
                        else if (int.TryParse(command.value, out var i)) _flagManager.SetFlag(command.key, i);
                    }
                    break;
            }
        }

        private static Color ParseColor(string name, Color fallback)
        {
            return name?.ToLowerInvariant() switch
            {
                "white" => Color.white, "black" => Color.black, "red" => Color.red,
                _ => ColorUtility.TryParseHtmlString(name, out var c) ? c : fallback
            };
        }

        private void OnDestroy() => StopEngine();
    }
}
