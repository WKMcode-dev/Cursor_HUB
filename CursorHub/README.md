# Cursor Hub 0.10.0

Aplicativo Windows x64 em C#/.NET 10 e osu!framework, com interface Kode Mono e assets SVG.

## Executavel unico

Baixe `CursorHub-0.10.0-win-x64.exe` na release. Pode mover somente esse arquivo para outra pasta ou para a area de trabalho. Ele inclui o runtime e as bibliotecas; os arquivos de apoio sao preparados automaticamente no perfil do usuario. Nao exige .NET ou Visual Studio instalados. Feche qualquer versao anterior antes de iniciar.

Presets, configuracoes e imagens importadas ficam em `%LOCALAPPDATA%\CursorHub`, portanto mover o executavel nao apaga os ajustes. Para transferir a colecao a outro computador, copie tambem esses dados.

## Personalizacao e presets

CURSOR e RASTRO tem tamanhos e cores independentes. Cada um aceita PNG/SVG ou formato circular. O editor de imagens permite giro em passos de 15 graus e inversao horizontal/vertical. No cursor, clique ou arraste a mira para selecionar o hotspot. As imagens mantem sua propria cor; a paleta se aplica ao formato circular.

No menu de tres pontos do preset, EDITAR abre abas CURSOR, RASTRO e AJUSTES. Edite nome, imagens, tamanhos, cores, hotspot, duracao, Hz e economia; SALVAR PRESET persiste todos os campos e atualiza a miniatura. CANCELAR descarta as mudancas do editor. As cores podem vir da paleta padrao ou do seletor RGB nativo. Os controles de cor aparecem apenas no formato circular; PNG/SVG preservam as cores da imagem. A galeria mostra somente os presets salvos e um card NOVO, sem placeholders vazios.

Os controles de tamanho, duracao e Hz usam o `slide_adjust.svg` fornecido, com bolinha e preenchimento dinamicos.

Clicar em um preset carrega seus ajustes na demonstracao. Ao alterar esses ajustes nas abas principais, SALVAR AJUSTES atualiza tambem o preset selecionado. A miniatura usa as imagens e cores salvas desse preset. Presets antigos conservam o visual ao migrar para os novos campos independentes.

PLAY ativa os efeitos no desktop. STOP, Ctrl+Shift+X ou fechar o app solicita sua interrupcao e a restauracao do cursor. Para aplicar novos ajustes a um efeito ja ativo, pare e pressione PLAY novamente.

## Janela e desempenho

Os controles geometricos ficam no recorte superior; Play/Stop fica no inferior direito. Maximizar preserva a proporcao da interface e o tamanho fisico do cursor. O tema muda imediatamente.

A captura usa historico real do Windows e um buffer fixo de particulas. O limite pode ser 256, 512 ou 1024. A camada nativa usa alfa por pixel, recebe somente a area do rastro e cresce conforme a trajetoria, com limite de memoria. Depois que o rastro expira, a camada deixa de ser redesenhada ate haver movimento. O limite de desenho e configuravel entre 30 e 360 Hz; a economia reduz a interface parada a 15 Hz.

A compatibilidade com aplicativos que desenham seu proprio cursor, jogos em tela cheia exclusiva e areas seguras do Windows nao e garantida. Consumo e latencia variam com o hardware.

## Executar o codigo-fonte

Com o SDK .NET 10 no Windows:

```powershell
dotnet run --project CursorHub/CursorHub.csproj
```

A execucao pelo codigo-fonte usa a pasta Assets. O workflow incorpora esses assets e publica o executavel unico com bibliotecas nativas incluidas. A fonte Kode Mono acompanha a licenca OFL; o atlas pode ser regenerado por Tools/Build-Font.py com Pillow 12.1.0.

## Verificacao

O workflow testa o executavel copiado para uma pasta que contem apenas ele. Os testes incluem abertura/cancelamento do seletor de arquivos, composicao do rastro sobre janelas independentes, Play/Stop rapido, interface nos dois temas, restauracao/maximizacao, migracao de presets, tamanhos independentes, edicao completa e recarga dos dados salvos. Capturas ficam nos artifacts de verificacao do GitHub Actions.
