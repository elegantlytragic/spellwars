using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Lidgren.Network;
using SpellWarsLibrary;
namespace SpellWarsClient
{
    public enum GameState
    {
        IPSelect,
        HeroSelect,
        Turn,
        NotTurn,
        Wait,
        NameSelect
    }
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SpriteFont font;
        MouseState prevMouse, mouse;
        int screenWidth = 1280, screenHeight = 720;

        NetClient client;
        const string ip = "127.0.0.1";
        const int port = 31337;
        string name;
        Textbox ipSelect, nameSelect;

        SourceCard[] sourceCards;
        QualityCard[] qualityCards;
        DeliveryCard[] deliveryCards;
        TreasureCard[] treasureCards;
        HeroCard[] heroCards;
        List<HeroCard> heroCardList;

        List<Card> drawPile;
        Card[] selectedCards = new Card[3];
        Player player;
        Dictionary<string, OtherPlayer> otherPlayers = new Dictionary<string, OtherPlayer>();

        Texture2D[] cardArt = new Texture2D[5];
        GameState gameState = GameState.IPSelect;
        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            graphics.PreferredBackBufferWidth = screenWidth;
            graphics.PreferredBackBufferHeight = screenHeight;
            this.Window.AllowUserResizing = true;
            this.Window.ClientSizeChanged += new EventHandler<EventArgs>(SizeChanged);
            this.IsMouseVisible = true;
        }
        protected override void Initialize()
        {
            base.Initialize();
            ipSelect = new Textbox(font, ip, Color.Black, Vector2.Zero);
            ipSelect.focused = true;
            nameSelect = new Textbox(font, "", Color.Black, Vector2.Zero);
            nameSelect.focused = true;

            client = new NetClient(new NetPeerConfiguration("SpellWars"));
        }
        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            sourceCards = Content.Load<SourceCard[]>("Cards/SourceCards");
            qualityCards = Content.Load<QualityCard[]>("Cards/QualityCards");
            deliveryCards = Content.Load<DeliveryCard[]>("Cards/DeliveryCards");
            treasureCards = Content.Load<TreasureCard[]>("Cards/TreasureCards");
            heroCards = Content.Load<HeroCard[]>("Cards/HeroCards");

            drawPile = AddDecks(sourceCards, qualityCards, deliveryCards);

            cardArt[0] = Content.Load<Texture2D>("Scans/cards1");
            cardArt[1] = Content.Load<Texture2D>("Scans/cards2");
            cardArt[2] = Content.Load<Texture2D>("Scans/deadwizard");
            cardArt[3] = Content.Load<Texture2D>("Scans/heroes");
            cardArt[4] = Content.Load<Texture2D>("Scans/treasure");

            heroCardList = AddDecksHero(heroCards);

            font = Content.Load<SpriteFont>("font");
        }
        protected override void UnloadContent()
        {
        }
        protected override void Update(GameTime gameTime)
        {
            KeyboardState keys = Keyboard.GetState();
            GamePadState pad = GamePad.GetState(PlayerIndex.One);
            mouse = Mouse.GetState();
            InputHelper help = new InputHelper();
            if (pad.Buttons.Back == ButtonState.Pressed || keys.IsKeyDown(Keys.Escape))
            {
                client.Disconnect("Game closed");
                this.Exit();
            }
            #region Server Logic
            NetIncomingMessage im;
            while ((im = client.ReadMessage()) != null)
            {
                switch (im.MessageType)
                {
                    case NetIncomingMessageType.Data:
                        switch (im.ReadString())
                        {
                            case "Handshake":
                                gameState = GameState.HeroSelect;
                                break;
                            case "GameStart":
                                gameState = GameState.NotTurn;
                                break;
                            case "DrawCard":
                                int cardsDrawn = im.ReadInt32();
                                for (int i = 0; i < cardsDrawn; i++)
                                {
                                    string cardName = im.ReadString();
                                    player.DrawCard(drawPile.Find(x => x.Name == cardName));
                                }
                                break;
                            case "AllPlayed":
                                int players = im.ReadInt32();
                                for (int i = 0; i < players; i++)
                                {
                                    string name = im.ReadString(),
                                           heroName = im.ReadString();
                                    int health = im.ReadInt32(),
                                        numberOfCards = im.ReadInt32();
                                    HeroCard hero = heroCardList.Find(x => x.Name == heroName);
                                    if (name == nameSelect.value) for (int n = 0; n < numberOfCards; n++) im.ReadString();
                                    else
                                    {
                                        if (otherPlayers.ContainsKey(name)) otherPlayers[name] = new OtherPlayer(hero, cardArt, name, health, numberOfCards);
                                        else otherPlayers.Add(name, new OtherPlayer(hero, cardArt, name, health, numberOfCards));
                                        for (int n = 0; n < numberOfCards; n++)
                                        {
                                            string cardName = im.ReadString();
                                            otherPlayers[name].Hand.Add(drawPile.Find(x => x.Name == cardName));
                                        }
                                    }
                                }
                                gameState = GameState.Turn;
                                break;
                        }
                        break;
                }
            }
            #endregion
            #region Game Logic
            switch (gameState)
            {
                case GameState.IPSelect:
                    ipSelect.Update(keys.GetPressedKeys());
                    if (keys.IsKeyDown(Keys.Enter))
                    {
                        client.Start();
                        NetOutgoingMessage hail = client.CreateMessage("Hail!");
                        client.Connect(ip, port, hail);
                    }
                    break;
                case GameState.HeroSelect:
                    Rectangle[] heroes = new Rectangle[8];
                    for (int i = 0; i < 8; i++) heroes[i] = new Rectangle((i % 4) * 250 + 100, (i / 4) * 181 + 100, 250, 181);
                    for (int i = 0; i < heroes.Length; i++) 
                    {
                        if (mouse.LeftButton == ButtonState.Pressed && heroes[i].Contains(new Point(mouse.X, mouse.Y)))
                        {
                            NetOutgoingMessage om = client.CreateMessage();
                            om.Write("HeroSelected");
                            om.Write(i);
                            client.SendMessage(om, NetDeliveryMethod.ReliableOrdered);
                            gameState = GameState.NameSelect;
                            player = new Player(heroCards[i], cardArt);
                        }
                    }
                    break;
                case GameState.NameSelect:
                    nameSelect.Update(keys.GetPressedKeys());
                    if (keys.IsKeyDown(Keys.Enter))
                    {
                        name = nameSelect.value;
                        NetOutgoingMessage om = client.CreateMessage();
                        om.Write("NameSelected");
                        om.Write(nameSelect.value);
                        client.SendMessage(om, NetDeliveryMethod.ReliableOrdered);
                        gameState = GameState.Wait;
                    }
                    break;
                case GameState.NotTurn:
                    bool swapping = false;
                    Card remove = new DeliveryCard(), swap = new DeliveryCard();
                    foreach (Card card in player.Hand)
                    {
                        if (card.MouseOver(new Point(mouse.X, mouse.Y)) && mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released)
                        {
                            int index = 0;
                            if (card.GetType() == typeof(SourceCard)) index = 0;
                            else if (card.GetType() == typeof(QualityCard)) index = 1;
                            else if (card.GetType() == typeof(DeliveryCard)) index = 2;
                            else continue;

                            if (selectedCards[index] != null) swapping = true;
                            swap = selectedCards[index];
                            selectedCards[index] = card;
                            remove = card;
                        }
                    }
                    player.Hand.Remove(remove);
                    if (swapping) player.Hand.Add(swap);

                    if (new Rectangle(screenWidth - 225, screenHeight - 144, 200, 119).Contains(new Point(mouse.X, mouse.Y)) && mouse.LeftButton == ButtonState.Pressed)
                    {
                        NetOutgoingMessage playMessage = client.CreateMessage();
                        playMessage.Write("Playing");
                        playMessage.Write(nameSelect.value);
                        foreach (Card card in selectedCards) playMessage.Write(card != null);
                        foreach (Card card in selectedCards) if (card != null) playMessage.Write(card.Name);
                        client.SendMessage(playMessage, NetDeliveryMethod.ReliableOrdered);
                        gameState = GameState.Wait;
                    }
                    break;
            }
            #endregion
            help.Update();
            prevMouse = mouse;
            base.Update(gameTime);
        }
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            switch (gameState)
            {
                case GameState.IPSelect:
                    ipSelect.Draw(spriteBatch);
                    break;
                case GameState.HeroSelect:
                    spriteBatch.DrawString(font, "Choose a Hero:", Vector2.Zero, Color.Black);
                    for (int i = 0; i < heroCards.Length; i++) heroCards[i].Draw(spriteBatch, cardArt, new Rectangle((i % 4) * 250 + 100, (i / 4) * 181 + 100, 250, 181));
                    break;
                case GameState.Wait:
                    spriteBatch.DrawString(font, "Waiting for a response from the server...", Vector2.Zero, Color.Black);
                    break;
                case GameState.NameSelect:
                    nameSelect.Draw(spriteBatch);
                    break;
                case GameState.NotTurn:
                    for (int i = 0; i < selectedCards.Length; i++)
                    {
                        if (selectedCards[i] != null)
                        {
                            if (!selectedCards[i].MouseOver(new Point(mouse.X, mouse.Y))) selectedCards[i].Draw(spriteBatch, cardArt, new Rectangle(screenWidth - 543 + (i * 181) - 25, screenHeight - 481, 181, 250));
                            else selectedCards[i].Draw(spriteBatch, cardArt, new Rectangle(screenWidth - 543 + (i * 362) - 25, screenHeight - 731, 362, 500));
                        }
                    }
                    player.DrawHand(spriteBatch, new Rectangle((screenWidth / 2) - 50 - (player.Hand.Count / 2 * 125), screenHeight - 225, 125, 200));
                    spriteBatch.Draw(Content.Load<Texture2D>("playbutton"), new Vector2(screenWidth - 225, screenHeight - 144), Color.White);
                    break;
                case GameState.Turn:
                    for (int i = 0; i < otherPlayers.Count; i++)
                    {
                        otherPlayers.ElementAt(i).Value.DrawHand(spriteBatch, new Rectangle((screenWidth / 2) - 50 - (otherPlayers.ElementAt(i).Value.Hand.Count / 2 * 125), i * 200, 125, 200));
                    }
                    player.DrawHand(spriteBatch, new Rectangle((screenWidth / 2) - 50 - (player.Hand.Count / 2 * 125), screenHeight - 225, 125, 200));
                    break;
            }
            spriteBatch.End();
            base.Draw(gameTime);
        }
        private List<Card> AddDecks(params Card[][] deck)
        {
            List<Card> retVal = new List<Card>();
            for (int i = 0; i < deck.Length; i++)
            {
                foreach (Card card in deck[i]) retVal.Add(card);
            }
            return retVal;
        }
        private List<HeroCard> AddDecksHero(params HeroCard[][] deck)
        {
            List<HeroCard> retVal = new List<HeroCard>();
            for (int i = 0; i < deck.Length; i++)
            {
                foreach (HeroCard card in deck[i]) retVal.Add(card);
            }
            return retVal;
        }
        private void SizeChanged(object sender, EventArgs e)
        {
            graphics.PreferredBackBufferHeight = screenHeight = Window.ClientBounds.Height;
            graphics.PreferredBackBufferWidth = screenWidth = Window.ClientBounds.Width;
            graphics.ApplyChanges();
        }
    }
}
