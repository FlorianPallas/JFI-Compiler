MOVE $s0, $sp
LI $t1, 0
SW $t1, 0($s0)
LI $t1, 1
LI $t2, 2
BEQ $t1, $t2, IF1
J IF2
IF1:
MOVE $s0, $sp
LI $t1, 1
SW $t1, 0($s0)

IF2:
