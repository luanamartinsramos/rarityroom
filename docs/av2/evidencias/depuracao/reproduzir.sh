#!/bin/bash
# Reproduz: carrinho com itens de DOIS vendedores → POST /api/pedidos
B=http://localhost:5055/api; D=$(mktemp -d)
c(){ curl -s -c $D/$1 -b $D/$1 "${@:2}"; }
for u in ana carlos bruno; do
  c $u -o /dev/null -H 'Content-Type: application/json' -d "{\"nome\":\"$u Teste\",\"email\":\"$u@rep.com\",\"senha\":\"senha123\"}" $B/autenticacao/cadastrar
  c $u -o /dev/null -H 'Content-Type: application/json' -d "{\"email\":\"$u@rep.com\",\"senha\":\"senha123\"}" $B/autenticacao/entrar
done
P1=$(c ana -H 'Content-Type: application/json' -d '{"nome":"Item da Ana","preco":10,"estoque":5,"categoria":"Funko Pop"}' $B/produtos | python3 -c 'import json,sys;print(json.load(sys.stdin)["id"])')
P2=$(c carlos -H 'Content-Type: application/json' -d '{"nome":"Item do Carlos","preco":20,"estoque":5,"categoria":"Funko Pop"}' $B/produtos | python3 -c 'import json,sys;print(json.load(sys.stdin)["id"])')
c bruno -o /dev/null -H 'Content-Type: application/json' -d "{\"produtoId\":$P1,\"quantidade\":1}" $B/carrinho/itens
c bruno -o /dev/null -H 'Content-Type: application/json' -d "{\"produtoId\":$P2,\"quantidade\":1}" $B/carrinho/itens
echo "== POST /api/pedidos (2 vendedores)"
c bruno -w '\nHTTP %{http_code}\n' -H 'Content-Type: application/json' -d '{"cep":"01310-100","logradouro":"Avenida Paulista","numero":"1000","bairro":"Bela Vista","cidade":"São Paulo","estado":"SP","formaPagamento":"Pix","chaveIdempotencia":"rep-1"}' $B/pedidos | python3 -c 'import sys;t=sys.stdin.read();print(t[:400] if "HTTP 5" in t else t.split("\n")[-2]+" | pedidos: "+str(t.count("\"codigo\":\"RR-")))'
