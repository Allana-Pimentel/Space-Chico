# Space Chico: O Pântano Bioluminescente 🚀🐱

> **Jogo 2D de Plataforma Retrô desenvolvido em Unity 6 (`6000.5.6f1`)**  
> *Projeto Acadêmico de Ciência da Computação / Jogos Digitais*

---

## 🎮 Sobre o Jogo

**Space Chico** conta a história do gatinho astronauta Chico, cuja nave sofre uma pane no propulsor quântico e cai em um misterioso planeta alienígena repleto de pântanos bioluminescentes. Para consertar sua nave e voltar para casa, Chico precisa explorar o pântano, coletar cristais energéticos, escalar vinhas, enfrentar criaturas gosmentas e derrotar o temível **Monstro do Pântano** para recuperar a peça da nave.

---

## ✨ Mecânicas Principais

- **Movimentação Ágil & Pulo Duplo:** Controle responsivo com inércia, amortecimento e pulo duplo acrobático com efeito de partículas de energia ciano.
- **Escalada em Vinhas:** Subida e descida suave em cipós bioluminescentes, com centralização automática e salto lateral para fora da vinha.
- **Mecânica de Pisão (Stomp):** Pise na cabeça dos inimigos para eliminá-los e ganhar um impulso vertical no ar.
- **Sistema de Cristais Quânticos (Opção B):** Colete 10 cristais pelo mapa para ganhar permanentemente **+1 Coração de Vida Máxima** e recuperar toda a saúde!
- **Boss Fight em 3 Fases:**
  - **Fase 1:** Investidas físicas e golpes terrestres.
  - **Fase 2:** Disparo de cuspe ácido telegráfico.
  - **Fase 3:** Ponto fraco vulnerável na cabeça — acerte 3 pisões para derrotar o monstro e recuperar o propulsor da nave!
- **Abertura Cinematográfica:** Cutscene inicial no estilo *Star Wars*, com texto inclinado em perspectiva 3D calculado em tempo real por malha vetorial, seguido da animação da nave caindo no planeta.

---

## ⌨️ Controles

| Ação | Teclas |
| :--- | :--- |
| **Mover / Andar** | `Setas Esquerda / Direita` ou `A / D` |
| **Pular / Pulo Duplo** | `Barra de Espaço` / `W` / `Seta Cima` |
| **Subir / Descer Vinhas** | `W / S` ou `Setas Cima / Baixo` |
| **Pular da Vinha** | `Barra de Espaço` + Direção |
| **Pausar o Jogo** | `Esc` |

---

## 🏛️ Arquitetura do Código

O projeto conta com **1.992 linhas de código C# limpo e modular** (cerca de 2.300 linhas brutas), estruturado em 29 scripts divididos por responsabilidade única:

```
Assets/_SpaceChico/Scripts/
├── Combat/          # Interface de dano (IDamageable), perigos e projéteis
├── Core/            # GameManager, AudioManager, CameraFollow, Parallax e VFX
├── Enemies/         # IA dos Slimes, BossSwamp (3 fases) e arena
├── Items/           # Coletáveis (Cristais, Frutas, Propulsor) e Vinhas
├── Player/          # PlayerController (física e pulo duplo) e PlayerStats
└── UI/              # Menus (Principal, Pausa, Game Over, Vitória) e Cutscene
```

---

## 🚀 Como Executar o Projeto

1. Clone o repositório:
   ```bash
   git clone https://github.com/SEU_USUARIO/SpaceChico.git
   ```
2. Abra o **Unity Hub** e adicione o projeto selecionando a pasta `SpaceChico`.
3. Certifique-se de usar o **Unity 6 (6000.5.6f1)** ou superior.
4. Abra a cena do menu em `Assets/_SpaceChico/Scenes/00_Intro.unity` ou a fase em `Assets/_SpaceChico/Scenes/01_Pantano.unity`.
5. Pressione **Play (▶)** para jogar!

---

## 📄 Licença

Projeto desenvolvido para fins educacionais e acadêmicos.
