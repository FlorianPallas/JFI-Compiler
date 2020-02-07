li $t1, 0
li $t2, 10

start:
blt $t1, $t2, body
j end

body:
addi $t1, $t1, 1
move $a0, $t1
li $v0, 1
syscall
j start

end: