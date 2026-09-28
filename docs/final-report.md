# Relatório Final - PTM-NONIO

## Tema

Aplicação de Gestão Automática de Tutoria e Mentoria.

## Objetivo

O projeto tem como objetivo disponibilizar uma plataforma web para apoiar a coordenação de programas de tutoria e mentoria académica. A solução permite registar utilizadores, distinguir perfis de aluno, mentor e administrador, gerar emparelhamentos automáticos com base em critérios de compatibilidade, acompanhar sessões e recolher feedback.

## Solução Implementada

A solução foi desenvolvida como uma aplicação ASP.NET Core Web API com frontend web integrado. A persistência é feita com SQLite e Entity Framework Core. A autenticação utiliza JWT e os principais fluxos são consumidos por uma interface HTML/CSS/JavaScript servida pela própria aplicação.

## Funcionalidades Principais

- Login com JWT e perfis de acesso.
- Gestão de utilizadores, alunos, mentores e roles.
- Perfis académicos de alunos com curso, ano, objetivos e média.
- Perfis de mentores com curso, especialização, áreas de experiência, disponibilidade, capacidade e experiência.
- Algoritmo de matching automático com score de compatibilidade.
- Criação de emparelhamentos manuais e automáticos.
- Agendamento e atualização de sessões de mentoria.
- Registo de feedback apenas em sessões concluídas.
- Dashboard com métricas agregadas, timeline e estado operacional.
- Interface web responsiva para demonstração.
- Documentação de arquitetura, casos de uso e validação funcional.

## Algoritmo de Matching

O matching considera vários critérios ponderados:

- Curso comum entre aluno e mentor.
- Sobreposição entre interesses do aluno e áreas de especialização do mentor.
- Compatibilidade de disponibilidade.
- Relação entre objetivos do aluno e especialização do mentor.
- Capacidade disponível do mentor.
- Reforço para alunos com maior necessidade de acompanhamento e mentores experientes.

O score final é limitado entre 0 e 100. Mentores sem capacidade disponível deixam de ser recomendados para novos emparelhamentos.

## Validação

A validação final incluiu:

- Compilação da solução.
- Verificação dos dados seed.
- Verificação de credenciais de demonstração.
- Teste do matching automático.
- Prevenção de criação repetida de matches ativos para o mesmo aluno.
- Validação de regras de sessões.
- Validação de feedback apenas para participantes de uma sessão concluída.

## Estado Final

O sistema encontra-se preparado para demonstração académica. Os objetivos previstos até à semana final estão cobertos: frontend funcional, ciclo completo com matching, sessões e feedback, métricas, documentação, poster, apresentação e validação funcional.

## Limitações e Trabalho Futuro

- Acrescentar testes automáticos por endpoint se for exigida cobertura formal mais extensa.
- Preparar deployment externo caso a apresentação peça acesso remoto.
- Evoluir o painel por perfis, mostrando a cada utilizador apenas a informação estritamente relevante.
- Acrescentar notificações automáticas por email para sessões marcadas.
