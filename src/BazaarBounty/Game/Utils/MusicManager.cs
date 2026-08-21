
using System;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;

namespace BazaarBounty
{
    public class MusicManager
    {
        public Song mainTheme;
        public Song stage1_bgm;
        public Song stage2_bgm;
        public Song defeat_bgm;
        public SoundEffect triumph_sound;
        private int stage_changed_flag = 0;
        public MusicManager(){
            MediaPlayer.Volume = 0.8f;
            Initialize();
            LoadContent();
        }
        public void Initialize(){
            stage_changed_flag = 0;
            MediaPlayer.IsRepeating = true;
        }
        public void LoadContent(){
            mainTheme = BazaarBountyGame.GetGameInstance().Content.Load<Song>("Musics/MainTheme");
            stage1_bgm = BazaarBountyGame.GetGameInstance().Content.Load<Song>("Musics/BGM_open-fields-aaron-paul-low");
            stage2_bgm = BazaarBountyGame.GetGameInstance().Content.Load<Song>("Musics/BGM_feast-days-simon-folwar");
            defeat_bgm = BazaarBountyGame.GetGameInstance().Content.Load<Song>("Musics/Defeat_BGM");
            triumph_sound = BazaarBountyGame.GetGameInstance().Content.Load<SoundEffect>("Musics/Triumph_BGM");
        }
        public void Update(MapStage? mapStage, int NextLevelNumber){
            if(mapStage == MapStage.Stage1 && stage_changed_flag == 0){
                stage_changed_flag = 1;
                MediaPlayer.Play(stage1_bgm);
            }
            if(NextLevelNumber == 12){
                MediaPlayer.Volume = (float)Math.Max(MediaPlayer.Volume*0.99, 0.3f);
            }
            else if(mapStage == MapStage.Stage2 && NextLevelNumber == 13 && stage_changed_flag == 1){
                MediaPlayer.Volume = 1.0f;
                stage_changed_flag = 2;
                MediaPlayer.Play(stage2_bgm);
            }
        }
    }
    
}