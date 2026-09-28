# Casos de Uso

## Atores

- `Administrador`
- `Aluno`
- `Mentor`

## Casos de uso principais

### Administrador

- autenticar-se na plataforma
- consultar métricas globais
- consultar alunos e mentores registados
- gerar emparelhamentos automáticos
- criar emparelhamentos manuais
- agendar sessões
- consultar feedback e progresso

### Aluno

- autenticar-se
- consultar o mentor atribuído
- acompanhar sessões planeadas
- submeter feedback após sessão

### Mentor

- autenticar-se
- consultar alunos atribuídos
- acompanhar a sua carga atual
- consultar agenda de sessões
- submeter feedback sobre sessões

## Fluxo principal de matching

1. O administrador seleciona um aluno.
2. O sistema calcula a compatibilidade com mentores disponíveis.
3. O algoritmo pondera curso, interesses, disponibilidade, especialização e carga atual.
4. O melhor mentor é sugerido.
5. O administrador confirma a geração do match.
6. O sistema guarda o emparelhamento e permite agendar sessões.
