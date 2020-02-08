ADDI $sp, $sp, 0
LI $t1, 0
SW $t1, 0($sp)
LI $t1, 1
LI $t2, 1
WHILE0:
BEQ $t1, $t2, WHILE1
J WHILE2
WHILE1:
ADDI $sp, $sp, -4
LI $t1, 1
SW $t1, 4($sp)
ADDI $sp, $sp, 4

J WHILE0
WHILE2:
ADDI $sp, $sp, 0
