Cursor Hub 0.8.2 — correcao da importacao de PNG/SVG

- Seletor nativo abre em uma thread STA dedicada, sem bloquear a atualizacao do app.
- Erros do dialogo sao diferenciados de cancelamento; cliques repetidos nao abrem seletores duplicados.
- Importacao processa a imagem fora da thread da interface e aplica a textura na thread correta.
- Icones de fechar, minimizar e maximizar centralizados geometricamente no SVG, mantendo o recorte.
- Teste Windows abre e cancela de verdade o seletor a partir de chamadas STA e MTA.

Extraia o ZIP inteiro e execute CursorHub.exe. Nao requer .NET ou Visual Studio instalados.
