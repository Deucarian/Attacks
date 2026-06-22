# Cooldown Guide

Attacks uses fixed integer ticks for cooldowns.

- `cooldownTicks > 0`: after a successful attack, the source must wait that many `Tick` calls.
- `cooldownTicks == 0`: the attack can be used repeatedly.
- `readyOnRegister == true`: the first attack is immediately available.
- `readyOnRegister == false`: the first attack starts on cooldown.

`AttackCooldownState` is snapshot-friendly and contains only remaining ticks.
